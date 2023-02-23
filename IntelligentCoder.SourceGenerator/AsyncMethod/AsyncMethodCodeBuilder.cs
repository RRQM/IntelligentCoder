using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentCoder
{
    /// <summary>
    /// RpcApi代码构建器
    /// </summary>
    internal sealed class AsyncMethodCodeBuilder : IEquatable<AsyncMethodCodeBuilder>
    {
        private readonly List<string> m_allMethodIds = new List<string>();
        private readonly Dictionary<string, TypedConstant> m_namedArguments;

        /// <summary>
        /// 接口符号
        /// </summary>
        private readonly INamedTypeSymbol m_namedTypeSymbol;

        private readonly List<string> m_needMethodIds = new List<string>();

        /// <summary>
        /// RpcApi代码构建器
        /// </summary>
        /// <param name="rpcApi"></param>
        public AsyncMethodCodeBuilder(INamedTypeSymbol namedTypeSymbol, Compilation compilation)
        {
            m_namedTypeSymbol = namedTypeSymbol;
            Compilation = compilation;
            AttributeData attributeData = namedTypeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodPosterAttributeTypeName);

            m_namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);

            if (this.m_namedArguments.TryGetValue("IgnoreMethods", out var typedConstant))
            {
                foreach (var item in typedConstant.Values)
                {
                    this.IgnoreMethods.Add(item.Value.ToString());
                }
            }
        }

        public IAssemblySymbol Assembly => this.m_namedTypeSymbol.ContainingAssembly;
        public Compilation Compilation { get; }

        public List<string> IgnoreMethods { get; } = new List<string>();

        /// <summary>
        /// using
        /// </summary>
        public IEnumerable<string> Usings
        {
            get
            {
                yield return "using System;";
                yield return "using System.Diagnostics;";
                yield return "using System.Threading.Tasks;";
            }
        }

        public int Deep()
        {
            if (this.m_namedArguments.TryGetValue("Deep", out var typedConstant))
            {
                if (typedConstant.Value is int deep)
                {
                    return deep;
                }
            }

            return 0;
        }

        public bool Equals(AsyncMethodCodeBuilder other)
        {
            return other.GetFileName() == this.GetFileName();
        }

        public string GetFileName()
        {
            return m_namedTypeSymbol.ToDisplayString() + "Generator";
        }

        public string GetPrecompile(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (this.m_namedArguments.TryGetValue("Precompile", out var typedConstant))
            {
                return typedConstant.Value?.ToString();
            }
            return null;
        }

        /// <summary>
        /// 转换为SourceText
        /// </summary>
        /// <returns></returns>
        public SourceText ToSourceText()
        {
            var code = ToString();
            return SourceText.From(code, Encoding.UTF8);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //Debugger.Launch();
            var builder = new StringBuilder();

            string precompile = null;
            if (this.m_namedArguments.TryGetValue("Precompile", out TypedConstant typedConstant))
            {
                precompile = typedConstant.Value?.ToString();
            }

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                builder.AppendLine();
                builder.AppendLine($"#if {precompile}");
            }

            foreach (var item in Usings)
            {
                builder.AppendLine(item);
            }

            if (GetNamespace() == null)
            {
                if (m_namedTypeSymbol.TypeKind == TypeKind.Interface)
                {
                    BuildIntereface(builder);
                }
                else
                {
                    BuildMethod(builder);
                }
            }
            else
            {
                builder.AppendLine($"namespace {GetNamespace()}");
                builder.AppendLine("{");
                if (m_namedTypeSymbol.TypeKind == TypeKind.Interface)
                {
                    BuildIntereface(builder);
                }
                else
                {
                    BuildMethod(builder);
                }
                builder.AppendLine("}");
            }

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                builder.AppendLine();
                builder.AppendLine("#endif");
            }
            // System.Diagnostics.Debugger.Launch();
            return builder.ToString();
        }

        private string BuildExtensionMethod(INamedTypeSymbol namedTypeSymbol, IMethodSymbol method)
        {
            //Debugger.Launch();

            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodAttributeTypeName);
            Dictionary<string, TypedConstant> namedArguments;
            if (attributeData is null)
            {
                namedArguments = new Dictionary<string, TypedConstant>();
            }
            else
            {
                namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);
            }

            string methodName = GetMethodName(method, namedArguments);
            string returnType = GetReturnType(method, namedArguments);
            string accessibility = GetAccessibility(method);
            string precompile = GetPrecompile(method, namedArguments);
            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();
            //以下生成异步
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }
            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"public static Task {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"public static Task {methodName}");
                }
            }
            else
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"public static Task<{returnType}> {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"public static Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"(");//方法参数
            codeString.Append($"this {namedTypeSymbol.ToDisplayString()} instance");//方法参数
            if (parameters.Length > 0)
            {
                codeString.Append(",");//方法参数
            }
            codeString.Append($"{string.Join(",", parameters.Select(a => $"{a.ToDisplayString()} {a.Name}"))}");//方法参数
            codeString.Append($")");//方法参数
            codeString.AppendLine("{");//方法开始

            if (method.ReturnsVoid)
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"instance.{method.Name}<{GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
                }
                else
                {
                    codeString.AppendLine($"instance.{method.Name}({string.Join(",", parameters.Select(a => a.Name))});");
                }
               
                codeString.AppendLine("});");
            }
            else
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"return instance.{method.Name}<{GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
                }
                else
                {
                    codeString.AppendLine($"return instance.{method.Name}({string.Join(",", parameters.Select(a => a.Name))});");
                }
               
                codeString.AppendLine("});");
            }
            codeString.AppendLine("}");
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#endif");
            }
            return codeString.ToString();
        }

        private void BuildIntereface(StringBuilder builder)
        {
            builder.AppendLine($"partial interface {GetClassName()}");
            builder.AppendLine("{");
            //Debugger.Launch();

            foreach (var method in FindAllMethods(this.m_namedTypeSymbol))
            {
                var methodCode = BuildMethodInterface(method);
                builder.AppendLine(methodCode);
            }

            builder.AppendLine("}");
        }

        private void BuildMethod(StringBuilder builder)
        {
            if (this.m_namedTypeSymbol.IsStatic)
            {
                builder.AppendLine($"static partial class {GetClassName()}");
            }
            else
            {
                builder.AppendLine($"partial class {GetClassName()}");
            }

            builder.AppendLine("{");
            //Debugger.Launch();

            if (this.m_namedArguments.TryGetValue("Target", out TypedConstant typedConstant))
            {
                if (typedConstant.Value is INamedTypeSymbol namedTypeSymbol)
                {
                    if (namedTypeSymbol.IsStatic)
                    {
                        foreach (var method in FindAllMethods(namedTypeSymbol).Where(a => IsPublic(a)))
                        {
                            var methodCode = BuildStaticMethod(namedTypeSymbol, method);
                            builder.AppendLine(methodCode);
                        }
                    }
                    else
                    {
                        foreach (var method in FindAllMethods(namedTypeSymbol).Where(a => IsPublic(a)))
                        {
                            if (!NewExists(method))
                            {
                                var methodCode = BuildExtensionMethod(namedTypeSymbol, method);
                                builder.AppendLine(methodCode);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var method in FindAllMethods(this.m_namedTypeSymbol))
                {
                    if (!NewExists(method))
                    {
                        var methodCode = BuildNormalMethod(method);
                        builder.AppendLine(methodCode);
                    }
                }
            }

            builder.AppendLine("}");
        }

        private string BuildMethodInterface(IMethodSymbol method)
        {
            //Debugger.Launch();
            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodAttributeTypeName);
            Dictionary<string, TypedConstant> namedArguments;
            if (attributeData is null)
            {
                namedArguments = new Dictionary<string, TypedConstant>();
            }
            else
            {
                namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);
            }
            //Debugger.Launch();

            string methodName = GetMethodName(method, namedArguments);
            string returnType = GetReturnType(method, namedArguments);
            string accessibility = GetAccessibility(method);
            string precompile = GetPrecompile(method, namedArguments);
            accessibility = accessibility == "public" ? string.Empty : accessibility;
            var parameters = method.Parameters;

            //生成开始
            var codeString = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }

            codeString.AppendLine(GetComments(method));

            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"{accessibility} Task {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{accessibility} Task {methodName}");
                }
            }
            else
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"{accessibility} Task<{returnType}> {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{accessibility} Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"({string.Join(",", parameters.Select(a => $"{a.ToDisplayString()} {a.Name}"))});");//方法参数

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#endif");
            }
            return codeString.ToString();
        }

        private string BuildNormalMethod(IMethodSymbol method)
        {
            //Debugger.Launch();
            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodAttributeTypeName);
            Dictionary<string, TypedConstant> namedArguments;
            if (attributeData is null)
            {
                namedArguments = new Dictionary<string, TypedConstant>();
            }
            else
            {
                namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);
            }

            string methodName = GetMethodName(method, namedArguments);
            string returnType = GetReturnType(method, namedArguments);
            string accessibility = GetAccessibility(method);
            string keyword = GetKeyword(method);
            string precompile = GetPrecompile(method, namedArguments);
            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }
            //以下生成异步
            codeString.AppendLine(GetComments(method));
            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"{GetAccessibility(method)} {keyword} Task {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{GetAccessibility(method)} {keyword} Task {methodName}");
                }
            }
            else
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"{GetAccessibility(method)} {keyword} Task<{returnType}> {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{GetAccessibility(method)} {keyword} Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"({string.Join(",", parameters.Select(a => $"{a.ToDisplayString()} {a.Name}"))})");//方法参数
            codeString.AppendLine("{");//方法开始

            if (method.ReturnsVoid)
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"{method.Name}<{GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
                }
                else
                {
                    codeString.AppendLine($"{method.Name}({string.Join(",", parameters.Select(a => a.Name))});");
                }
               
                codeString.AppendLine("});");
            }
            else
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"return {method.Name}<{GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
                }
                else
                {
                    codeString.AppendLine($"return {method.Name}({string.Join(",", parameters.Select(a => a.Name))});");
                }
               
                codeString.AppendLine("});");
            }
            codeString.AppendLine("}");
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#endif");
            }
            return codeString.ToString();
        }

        private string BuildStaticMethod(INamedTypeSymbol namedTypeSymbol, IMethodSymbol method)
        {
            //Debugger.Launch();

            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodAttributeTypeName);
            Dictionary<string, TypedConstant> namedArguments;
            if (attributeData is null)
            {
                namedArguments = new Dictionary<string, TypedConstant>();
            }
            else
            {
                namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);
            }

            string methodName = GetMethodName(method, namedArguments);
            string returnType = GetReturnType(method, namedArguments);
            string accessibility = GetAccessibility(method);
            string precompile = GetPrecompile(method, namedArguments);
            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();
            //以下生成异步
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }
            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"public static Task {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"public static Task {methodName}");
                }
            }
            else
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"public static Task<{returnType}> {methodName}<{GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"public static Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"(");//方法参数
            codeString.Append($"{string.Join(",", parameters.Select(a => $"{a.ToDisplayString()} {a.Name}"))}");//方法参数
            codeString.Append($")");//方法参数
            codeString.AppendLine("{");//方法开始

            if (method.ReturnsVoid)
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"{namedTypeSymbol.ToDisplayString()}.{method.Name}<{GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
                }
                else 
                {
                    codeString.AppendLine($"{namedTypeSymbol.ToDisplayString()}.{method.Name}({string.Join(",", parameters.Select(a => a.Name))});");
                }
               
                codeString.AppendLine("});");
            }
            else
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"return {namedTypeSymbol.ToDisplayString()}.{method.Name}<{GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
                }
                else
                {
                    codeString.AppendLine($"return {namedTypeSymbol.ToDisplayString()}.{method.Name}({string.Join(",", parameters.Select(a => a.Name))});");
                }
               
                codeString.AppendLine("});");
            }
            codeString.AppendLine("}");
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#endif");
            }
            return codeString.ToString();
        }

        private List<IMethodSymbol> FindAllMethods(INamedTypeSymbol namedTypeSymbol)
        {
            int deep = Deep();
            List<IMethodSymbol> methods = new List<IMethodSymbol>();
            FindMethods(namedTypeSymbol, methods, ref deep);
            return methods;
        }

        private void FindMethods(INamedTypeSymbol namedTypeSymbol, List<IMethodSymbol> methods,ref int deep)
        {
            var list = namedTypeSymbol
               .GetMembers()
               .OfType<IMethodSymbol>()
               .Where(a =>
               {
                   if (a.MethodKind != MethodKind.Ordinary)
                   {
                       return false;
                   }
                   
                   string id = GetMethodId(a);
                   if (namedTypeSymbol.IsAbstract && !m_allMethodIds.Contains(id))
                   {
                       return false;
                   }

                   if (m_allMethodIds.Contains(id))
                   {
                       return false;
                   }
                   m_allMethodIds.Add(id);

                   if (IsAsync(a))
                   {
                       return false;
                   }

                   if (this.IgnoreMethods.Contains(a.Name))
                   {
                       return false;
                   }
                   switch (a.DeclaredAccessibility)
                   {
                       case Accessibility.Private:
                           if (!SymbolEqualityComparer.Default.Equals(namedTypeSymbol, this.m_namedTypeSymbol))
                           {
                               return false;
                           }
                           break;

                       case Accessibility.ProtectedOrInternal:
                       case Accessibility.ProtectedAndInternal:
                       case Accessibility.Internal:
                           if (!SymbolEqualityComparer.Default.Equals(a.ContainingAssembly, this.Assembly))
                           {
                               return false;
                           }
                           break;

                       case Accessibility.Protected:
                       case Accessibility.Public:
                           break;

                       default:
                           return false;
                   }
                  
                   if (!a.ReturnsVoid)
                   {
                       if (!IsPublic(a.ReturnType))
                       {
                           if (!SymbolEqualityComparer.Default.Equals(a.ReturnType.ContainingAssembly, this.Compilation.Assembly))
                           {
                               return false;
                           }
                       }
                   }


                   foreach (var item in a.Parameters)
                   {
                       if (item.RefKind != RefKind.None)
                       {
                           return false;
                       }
                       if (!IsPublic(item.Type))
                       {
                           if (!SymbolEqualityComparer.Default.Equals(item.Type.ContainingAssembly, this.Assembly))
                           {
                               return false;
                           }
                       }
                   }

                   if (m_needMethodIds.Contains(id))
                   {
                       return false;
                   }
                   m_needMethodIds.Add(id);
                   return true;
               });

            methods.AddRange(list);

            if (namedTypeSymbol.IsStatic)
            {
                return;
            }
            if (--deep < 0)
            {
                return;
            }

            if (namedTypeSymbol.BaseType != null)
            {
                if (IsPublic(namedTypeSymbol.BaseType) || SymbolEqualityComparer.Default.Equals(namedTypeSymbol.BaseType.ContainingAssembly, this.Assembly))
                {
                    FindMethods(namedTypeSymbol.BaseType, methods, ref deep);
                }
            }

            foreach (var item in namedTypeSymbol.Interfaces)
            {
                if (IsPublic(item) || SymbolEqualityComparer.Default.Equals(item.ContainingAssembly, this.Compilation.Assembly))
                {
                    FindMethods(item, methods, ref deep);
                }
            }
        }

        private string GetAccessibility(ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return "private";

                case Accessibility.ProtectedAndInternal:
                    return "protected internal";

                case Accessibility.Protected:
                    return "protected";

                case Accessibility.Internal:
                    return "internal";

                case Accessibility.ProtectedOrInternal:
                    return "protected internal";

                case Accessibility.Public:
                default:
                    return "public";
            }
        }

        private string GetClassName()
        {
            return m_namedTypeSymbol.Name;
        }

        private string GetComments(IMethodSymbol method)
        {
            return string.Empty;
        }

        private string GetGenericType(IMethodSymbol method)
        {
            return string.Join(",", method.TypeParameters.Select(a => a.ToDisplayString()));
        }

        private string GetKeyword(IMethodSymbol method)
        {
            if (method.IsStatic)
            {
                return "static";
            }

            if (method.IsAbstract || method.IsVirtual)
            {
                return "virtual";
            }

            return string.Empty;
        }

        private string GetMethodId(IMethodSymbol method)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(method.Name);
            foreach (var item in method.Parameters)
            {
                stringBuilder.Append(item.ToDisplayString());
            }
            return stringBuilder.ToString();
        }

        private string GetMethodName(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (namedArguments.TryGetValue("Template", out var typedConstant))
            {
                return string.Format(typedConstant.Value.ToString(), method.Name);
            }
            else if (m_namedArguments.TryGetValue("Template", out typedConstant))
            {
                return string.Format(typedConstant.Value.ToString(), method.Name);
            }
            else
            {
                return method.Name + "Async";
            }
        }

        private string GetMethodName(IMethodSymbol method)
        {
            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodAttributeTypeName);
            Dictionary<string, TypedConstant> namedArguments;
            if (attributeData is null)
            {
                namedArguments = new Dictionary<string, TypedConstant>();
            }
            else
            {
                namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);
            }
            if (namedArguments.TryGetValue("Template", out var typedConstant))
            {
                return string.Format(typedConstant.Value.ToString(), method.Name);
            }
            else if (m_namedArguments.TryGetValue("Template", out typedConstant))
            {
                return string.Format(typedConstant.Value.ToString(), method.Name);
            }
            else
            {
                return method.Name + "Async";
            }
        }

        private string GetNamespace()
        {
            int r = this.m_namedTypeSymbol.ToDisplayString().LastIndexOf('.');
            if (r > 0)
            {
                return this.m_namedTypeSymbol.ToDisplayString().Substring(0, r);
            }
            return null;
        }

        private string GetNewMethodId(IMethodSymbol method)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(GetMethodName(method));
            foreach (var item in method.Parameters)
            {
                stringBuilder.Append(item.ToDisplayString());
            }
            return stringBuilder.ToString();
        }

        private string GetReturnType(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            return method.ReturnType.ToDisplayString();
        }

        private bool HasFlags(int value, int flag)
        {
            return (value & flag) == flag;
        }

        private bool IsAsync(IMethodSymbol method)
        {
            if (method.IsAsync)
            {
                return true;
            }
            if (method.Name.EndsWith("Async"))
            {
                return true;
            }

            if (method.ReturnsVoid)
            {
                return false;
            }
            else if (method.ReturnType.ToDisplayString().Contains(typeof(ValueTask).FullName) ||
                method.ReturnType.ToDisplayString().Contains(typeof(Task).FullName))
            {
                return true;
            }

            return false;
        }

        private bool IsAsync(MethodInfo methodInfo)
        {
            if (methodInfo.Name.EndsWith("Async"))
            {
                return true;
            }

            if (methodInfo.ReturnType == null)
            {
                return false;
            }
            else if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType) || methodInfo.ReturnType.FullName.Contains(typeof(ValueTask).FullName))
            {
                return true;
            }

            return false;
        }

        private bool IsInternal(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Internal;
        }

        private bool IsPrivate(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Private;
        }

        private bool IsPublic(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        //private bool IsPublicOrProtected(IMethodSymbol method)
        //{
        //    return method.DeclaredAccessibility == Accessibility.Public||
        //        method.DeclaredAccessibility== Accessibility.Protected||
        //        method.DeclaredAccessibility== Accessibility.ProtectedOrInternal;
        //}

        //private bool IsPublic(ITypeSymbol typeSymbol)
        //{
        //    if (typeSymbol.DeclaredAccessibility!= Accessibility.Public)
        //    {
        //        return false;
        //    }
        //    if (typeSymbol.ContainingType!=null)
        //    {
        //        return IsPublic(typeSymbol.ContainingType);
        //    }
        //    return true ;
        //}

        private bool NewExists(IMethodSymbol method)
        {
            return m_allMethodIds.Contains(GetNewMethodId(method));
        }
    }
}