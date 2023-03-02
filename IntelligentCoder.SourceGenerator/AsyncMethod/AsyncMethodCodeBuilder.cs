using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        /// <summary>
        /// 接口符号
        /// </summary>
        private readonly INamedTypeSymbol m_namedTypeSymbol;

        private List<string> m_allMethodIds;
        private List<string> m_ignoreMethods;
        private Dictionary<string, TypedConstant> m_namedArguments;
        private List<string> m_needMethodIds;

        /// <summary>
        /// RpcApi代码构建器
        /// </summary>
        /// <param name="rpcApi"></param>
        public AsyncMethodCodeBuilder(INamedTypeSymbol namedTypeSymbol, Compilation compilation)
        {
            this.m_namedTypeSymbol = namedTypeSymbol;
            this.Compilation = compilation;
        }

        public IAssemblySymbol Assembly => this.m_namedTypeSymbol.ContainingAssembly;
        public Compilation Compilation { get; }

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

            return (this.m_namedTypeSymbol.ToDisplayString() + "Generator.g.cs").Replace("<", "").Replace(">", "");
        }

        public MethodDeclarationSyntax GetMethodDeclaration(IMethodSymbol method)
        {
            var meth = (method.PartialImplementationPart != null) ? method.PartialImplementationPart : method;
            var declarings = meth.DeclaringSyntaxReferences;
            if (declarings == null || declarings.Count() == 0) return null;
            return declarings.First().GetSyntax() as MethodDeclarationSyntax;
        }

        public string GetPrecompile(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (this.m_namedArguments.TryGetValue("Precompile", out var typedConstant))
            {
                return null;
            }
            if (namedArguments.TryGetValue("Precompile", out typedConstant))
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
            var TextFormat = CSharpSyntaxTree.ParseText(this.ToString(), new CSharpParseOptions(LanguageVersion.CSharp8)).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
            return SourceText.From(TextFormat, Encoding.UTF8);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var item in this.Usings)
            {
                builder.AppendLine(item);
            }
            foreach (var attributeData in this.m_namedTypeSymbol.GetAttributes().Where(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.AsyncMethodPosterAttributeTypeName))
            {
                this.m_ignoreMethods = new List<string>();
                this.m_allMethodIds = new List<string>();
                this.m_needMethodIds = new List<string>();

                this.m_namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);

                if (this.m_namedArguments.TryGetValue("IgnoreMethods", out var typedConstant))
                {
                    foreach (var item in typedConstant.Values)
                    {
                        this.m_ignoreMethods.Add(item.Value.ToString());
                    }
                }

                //Debugger.Launch();

                string precompile = null;
                if (this.m_namedArguments.TryGetValue("Precompile", out typedConstant))
                {
                    precompile = typedConstant.Value?.ToString();
                }

                if (!string.IsNullOrWhiteSpace(precompile))
                {
                    builder.AppendLine();
                    builder.AppendLine($"#if {precompile}");
                }

                if (this.GetNamespace() == null)
                {
                    if (this.IsInterface(this.m_namedTypeSymbol))
                    {
                        this.BuildIntereface(builder);
                    }
                    else
                    {
                        this.BuildClass(builder);
                    }
                }
                else
                {
                    builder.AppendLine($"namespace {this.GetNamespace()}");
                    builder.AppendLine("{");
                    if (this.IsInterface(this.m_namedTypeSymbol))
                    {
                        this.BuildIntereface(builder);
                    }
                    else
                    {
                        this.BuildClass(builder);
                    }
                    builder.AppendLine("}");
                }

                if (!string.IsNullOrWhiteSpace(precompile))
                {
                    builder.AppendLine();
                    builder.AppendLine("#endif");
                }
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

            var methodName = this.GetMethodName(method, namedArguments);
            var returnType = this.GetReturnType(method, namedArguments);
            var accessibility = this.GetAccessibility(method);
            var precompile = this.GetPrecompile(method, namedArguments);
            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();
            //以下生成异步
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }
            codeString.AppendLine(this.GetComments(method));
            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"public static Task {methodName}<{this.GetGenericType(method)}>");
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
                    codeString.Append($"public static Task<{returnType}> {methodName}<{this.GetGenericType(method)}>");
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
            codeString.Append($"{string.Join(",", parameters.Select(a => $"{a.Type.ToDisplayString()} {a.Name}"))}");//方法参数
            codeString.Append($"){this.GetConstraintClauses(method)}");//方法参数
            codeString.AppendLine("{");//方法开始

            if (method.ReturnsVoid)
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"instance.{method.Name}<{this.GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
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
                    codeString.AppendLine($"return instance.{method.Name}<{this.GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
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

        private string GetGenericTypeString(INamedTypeSymbol namedTypeSymbol)
        {
            if (!namedTypeSymbol.IsGenericType)
            {
                return string.Empty;
            }
            return $"<{this.GetGenericType(namedTypeSymbol)}>";
        }

        private void BuildIntereface(StringBuilder builder)
        {
            if (this.m_namedTypeSymbol.IsGenericType)
            {
                //Debugger.Launch();
                builder.AppendLine($"partial interface {this.GetClassName()} <{this.GetGenericType(this.m_namedTypeSymbol)}>");
            }
            else
            {
                builder.AppendLine($"partial interface {this.GetClassName()}");
            }

            builder.AppendLine("{");
            //Debugger.Launch();

            foreach (var method in this.FindAllMethods(this.m_namedTypeSymbol))
            {
                if (!this.NewExists(method))
                {
                    var methodCode = this.BuildMethodInterface(method);
                    builder.AppendLine(methodCode);
                }
            }

            builder.AppendLine("}");
        }

        private void BuildClass(StringBuilder builder)
        {
            if (this.m_namedTypeSymbol.IsStatic)
            {
                if (this.m_namedTypeSymbol.IsGenericType)
                {
                    builder.AppendLine($"static partial class {this.GetClassName()} <{this.GetGenericType(this.m_namedTypeSymbol)}>");
                }
                else
                {
                    builder.AppendLine($"static partial class {this.GetClassName()}");
                }
            }
            else
            {
                if (this.m_namedTypeSymbol.IsGenericType)
                {
                    builder.AppendLine($"partial class {this.GetClassName()} <{this.GetGenericType(this.m_namedTypeSymbol)}>");
                }
                else
                {
                    builder.AppendLine($"partial class {this.GetClassName()}");
                }
            }

            builder.AppendLine("{");
            //Debugger.Launch();

            if (this.m_namedArguments.TryGetValue("Target", out var typedConstant))
            {
                if (typedConstant.Value is INamedTypeSymbol namedTypeSymbol)
                {
                    if (namedTypeSymbol.IsStatic)
                    {
                        foreach (var method in this.FindAllMethods(namedTypeSymbol).Where(a => this.IsPublic(a)))
                        {
                            if (!this.NewExists(method))
                            {
                                var methodCode = this.BuildStaticMethod(namedTypeSymbol, method);
                                builder.AppendLine(methodCode);
                            }
                        }
                    }
                    else
                    {
                        foreach (var method in this.FindAllMethods(namedTypeSymbol).Where(a => this.IsPublic(a)))
                        {
                            if (!this.NewExists(method))
                            {
                                var methodCode = this.BuildExtensionMethod(namedTypeSymbol, method);
                                builder.AppendLine(methodCode);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var method in this.FindAllMethods(this.m_namedTypeSymbol))
                {
                    if (!this.NewExists(method))
                    {
                        var methodCode = this.BuildNormalMethod(method);
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

            var methodName = this.GetMethodName(method, namedArguments);
            var returnType = this.GetReturnType(method, namedArguments);
            var accessibility = this.GetAccessibility(method);
            var precompile = this.GetPrecompile(method, namedArguments);
            accessibility = accessibility == "public" ? string.Empty : accessibility;
            var parameters = method.Parameters;

            //生成开始
            var codeString = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }

            codeString.AppendLine(this.GetComments(method));

            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    //Debugger.Launch();
                    var attrib = method.GetAttributes();
                    codeString.Append($"{accessibility} Task {methodName}<{this.GetGenericType(method)}>");
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
                    codeString.Append($"{accessibility} Task<{returnType}> {methodName}<{this.GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{accessibility} Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"({string.Join(",", parameters.Select(a => $"{a.Type.ToDisplayString()} {a.Name}"))}){this.GetConstraintClauses(method)};");//方法参数

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

            var methodName = this.GetMethodName(method, namedArguments);
            var returnType = this.GetReturnType(method, namedArguments);
            var accessibility = this.GetAccessibility(method);
            var keyword = this.GetKeyword(method);
            var precompile = this.GetPrecompile(method, namedArguments);
            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }
            //以下生成异步
            codeString.AppendLine(this.GetComments(method));
            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"{this.GetAccessibility(method)} {keyword} Task {methodName}<{this.GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{this.GetAccessibility(method)} {keyword} Task {methodName}");
                }
            }
            else
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"{this.GetAccessibility(method)} {keyword} Task<{returnType}> {methodName}<{this.GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"{this.GetAccessibility(method)} {keyword} Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"({string.Join(",", parameters.Select(a => $"{a.Type.ToDisplayString()} {a.Name}"))}){this.GetConstraintClauses(method)}");//方法参数
            codeString.AppendLine("{");//方法开始

            if (method.ReturnsVoid)
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"{method.Name}<{this.GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
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
                    codeString.AppendLine($"return {method.Name}<{this.GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
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

            var methodName = this.GetMethodName(method, namedArguments);
            var returnType = this.GetReturnType(method, namedArguments);
            var accessibility = this.GetAccessibility(method);
            var precompile = this.GetPrecompile(method, namedArguments);
            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();
            //以下生成异步
            if (!string.IsNullOrWhiteSpace(precompile))
            {
                codeString.AppendLine();
                codeString.AppendLine($"#if {precompile}");
            }
            codeString.AppendLine(this.GetComments(method));
            if (method.ReturnsVoid)
            {
                if (method.IsGenericMethod)
                {
                    codeString.Append($"public static Task {methodName}<{this.GetGenericType(method)}>");
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
                    codeString.Append($"public static Task<{returnType}> {methodName}<{this.GetGenericType(method)}>");
                }
                else
                {
                    codeString.Append($"public static Task<{returnType}> {methodName}");
                }
            }

            codeString.Append($"(");//方法参数
            codeString.Append($"{string.Join(",", parameters.Select(a => $"{a.Type.ToDisplayString()} {a.Name}"))}");//方法参数
            codeString.Append($"){this.GetConstraintClauses(method)}");//方法参数
            codeString.AppendLine("{");//方法开始

            if (method.ReturnsVoid)
            {
                codeString.AppendLine("return Task.Run(() => ");
                codeString.AppendLine("{");
                if (method.IsGenericMethod)
                {
                    codeString.AppendLine($"{namedTypeSymbol.ToDisplayString()}.{method.Name}<{this.GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
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
                    codeString.AppendLine($"return {namedTypeSymbol.ToDisplayString()}.{method.Name}<{this.GetGenericType(method)}>({string.Join(",", parameters.Select(a => a.Name))});");
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
            var deep = this.Deep();
            var methods = new List<IMethodSymbol>();
            this.FindMethods(namedTypeSymbol, methods, ref deep);
            return methods;
        }

        private void FindMethods(INamedTypeSymbol namedTypeSymbol, List<IMethodSymbol> methods, ref int deep)
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

                   var id = this.GetMethodId(a);
                   if (this.m_allMethodIds.Contains(id))
                   {
                       return false;
                   }
                   this.m_allMethodIds.Add(id);

                   if (this.IsAsync(a))
                   {
                       return false;
                   }

                   if (this.m_ignoreMethods.Contains(a.Name))
                   {
                       return false;
                   }
                   if (this.IsIgnore(a))
                   {
                       return false;
                   }
                   var flags = this.GetMethodFlags();
                   switch (a.DeclaredAccessibility)
                   {
                       case Accessibility.Private:
                           if (!flags.HasFlag(MemberFlags.Private))
                           {
                               return false;
                           }
                           if (!SymbolEqualityComparer.Default.Equals(namedTypeSymbol, this.m_namedTypeSymbol))
                           {
                               return false;
                           }
                           break;

                       case Accessibility.ProtectedOrInternal:
                       case Accessibility.ProtectedAndInternal:
                       case Accessibility.Internal:
                           if (!flags.HasFlag(MemberFlags.Internal))
                           {
                               return false;
                           }
                           if (!SymbolEqualityComparer.Default.Equals(a.ContainingAssembly, this.Assembly))
                           {
                               return false;
                           }
                           break;

                       case Accessibility.Protected:
                           {
                               if (!flags.HasFlag(MemberFlags.Protected))
                               {
                                   return false;
                               }
                               break;
                           }
                       case Accessibility.Public:
                           {
                               if (!flags.HasFlag(MemberFlags.Public))
                               {
                                   return false;
                               }
                           }
                           break;

                       default:
                           return false;
                   }

                   if (!a.ReturnsVoid)
                   {
                       if (!this.IsPublic(a.ReturnType))
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
                       if (item.Type.IsRefLikeType)
                       {
                           return false;
                       }
                       if (!this.IsPublic(item.Type))
                       {
                           if (!SymbolEqualityComparer.Default.Equals(item.Type.ContainingAssembly, this.Assembly))
                           {
                               return false;
                           }
                       }
                   }

                   if (this.m_needMethodIds.Contains(id))
                   {
                       return false;
                   }
                   this.m_needMethodIds.Add(id);
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
                if (this.IsPublic(namedTypeSymbol.BaseType) || SymbolEqualityComparer.Default.Equals(namedTypeSymbol.BaseType.ContainingAssembly, this.Assembly))
                {
                    this.FindMethods(namedTypeSymbol.BaseType, methods, ref deep);
                }
            }

            //foreach (var item in namedTypeSymbol.Interfaces)
            //{
            //    if (IsPublic(item) || SymbolEqualityComparer.Default.Equals(item.ContainingAssembly, this.Compilation.Assembly))
            //    {
            //        FindMethods(item, methods, ref deep);
            //    }
            //}
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
            return this.m_namedTypeSymbol.Name;
        }

        private string GetComments(IMethodSymbol method)
        {
            var cref = method.ToDisplayString().Replace("<", "{").Replace(">", "}");
            return $"/// <inheritdoc cref=\"{cref}\"/>";
        }

        private bool IsIgnore(IMethodSymbol method)
        {
            return method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == "IntelligentCoder.AsyncMethodIgnoreAttribute") is not null;
        }

        private string GetConstraintClauses(IMethodSymbol method)
        {
            if (!method.IsGenericMethod)
            {
                return string.Empty;
            }
            var syntaxNode = this.GetMethodDeclaration(method);
            if (syntaxNode == null)
            {
                return string.Empty;
            }
            if (syntaxNode.ConstraintClauses.Count == 0)
            {
                return string.Empty;
            }
            return syntaxNode.ConstraintClauses.ToFullString();
        }

        private string GetGenericType(IMethodSymbol method)
        {
            return string.Join(",", method.TypeParameters.Select(a => a.ToDisplayString()));
        }

        private string GetGenericType(INamedTypeSymbol namedTypeSymbol)
        {
            return string.Join(",", namedTypeSymbol.TypeParameters.Select(a => a.ToDisplayString()));
        }

        private string GetKeyword(IMethodSymbol method)
        {
            if (method.IsStatic)
            {
                return "static";
            }
            if (this.m_namedTypeSymbol.IsSealed)
            {
                return string.Empty;
            }
            if (method.IsAbstract || method.IsVirtual)
            {
                return "virtual";
            }

            return string.Empty;
        }

        private string GetMethodId(IMethodSymbol method)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(method.Name);
            foreach (var item in method.Parameters)
            {
                stringBuilder.Append(item.Type.ToDisplayString());
            }
            return stringBuilder.ToString();
        }

        private string GetMethodName(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (namedArguments.TryGetValue("Template", out var typedConstant))
            {
                return string.Format(typedConstant.Value.ToString(), method.Name);
            }
            else if (this.m_namedArguments.TryGetValue("Template", out typedConstant))
            {
                return string.Format(typedConstant.Value.ToString(), method.Name);
            }
            else
            {
                return method.Name + "Async";
            }
        }

        private MemberFlags GetMethodFlags()
        {
            if (this.m_namedArguments.TryGetValue("Flags", out var typedConstant))
            {
                return (MemberFlags)typedConstant.Value;
            }
            else
            {
                return MemberFlags.Internal | MemberFlags.Public | MemberFlags.Protected | MemberFlags.Private;
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
            else if (this.m_namedArguments.TryGetValue("Template", out typedConstant))
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
            var r = this.m_namedTypeSymbol.ToDisplayString().LastIndexOf('.');
            if (r > 0)
            {
                return this.m_namedTypeSymbol.ToDisplayString().Substring(0, r);
            }
            return null;
        }

        private string GetNewMethodId(IMethodSymbol method)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetMethodName(method));
            foreach (var item in method.Parameters)
            {
                stringBuilder.Append(item.Type.ToDisplayString());
            }
            return stringBuilder.ToString();
        }

        private string GetReturnType(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            return method.ReturnType.ToDisplayString();
        }

        private string GetTypeConstraintClauses(INamedTypeSymbol namedTypeSymbol)
        {
            if (!namedTypeSymbol.IsGenericType)
            {
                return string.Empty;
            }
            foreach (var item in namedTypeSymbol.DeclaringSyntaxReferences)
            {
                if (item.GetSyntax() is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    if (typeDeclarationSyntax.ConstraintClauses.Count > 0)
                    {
                        return typeDeclarationSyntax.ConstraintClauses.ToFullString();
                    }
                }
            }

            return string.Empty;
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

        private bool IsInterface(INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.TypeKind == TypeKind.Interface;
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
            return this.m_allMethodIds.Contains(this.GetNewMethodId(method));
        }
    }
}