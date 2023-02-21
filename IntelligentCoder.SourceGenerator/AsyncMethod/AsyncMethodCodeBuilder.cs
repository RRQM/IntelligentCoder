using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntelligentCoder
{
    /// <summary>
    /// RpcApi代码构建器
    /// </summary>
    internal sealed class AsyncMethodCodeBuilder
    {
        private readonly Dictionary<string, TypedConstant> m_namedArguments;

        /// <summary>
        /// 接口符号
        /// </summary>
        private readonly INamedTypeSymbol m_namedTypeSymbol;
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
        }

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

        public Compilation Compilation { get; }

        public string GetFileName()
        {
            return m_namedTypeSymbol.ToDisplayString() + "Generator";
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
            foreach (var item in Usings)
            {
                builder.AppendLine(item);
            }

            if (GetNamespace() == null)
            {
                if (m_namedTypeSymbol.IsAbstract)
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
                if (m_namedTypeSymbol.IsAbstract)
                {
                    BuildIntereface(builder);
                }
                else
                {
                    BuildMethod(builder);
                }
                builder.AppendLine("}");
            }


            // System.Diagnostics.Debugger.Launch();
            return builder.ToString();
        }

        private void BuildIntereface(StringBuilder builder)
        {
            var interfaceNames = new List<string>();

            builder.AppendLine($"partial interface {GetClassName()}");
            builder.AppendLine("{");
            //Debugger.Launch();

            foreach (var method in FindMethods())
            {
                var methodCode = BuildMethodInterface(method);
                builder.AppendLine(methodCode);
            }

            builder.AppendLine("}");
        }

        private void BuildMethod(StringBuilder builder)
        {
            builder.AppendLine($"partial class {GetClassName()}");
            builder.AppendLine("{");
            //Debugger.Launch();

            foreach (var method in FindMethods())
            {
                var methodCode = BuildMethod(method);
                builder.AppendLine(methodCode);
            }

            builder.AppendLine("}");
        }

        private string GetAccessibility(IMethodSymbol method)
        {
            switch (method.DeclaredAccessibility)
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
     
        private string BuildMethod(IMethodSymbol method)
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

            var parameters = method.Parameters;
            //生成开始
            var codeString = new StringBuilder();
            //以下生成异步
            codeString.AppendLine(GetComments(method));
            if (method.ReturnsVoid)
            {
                codeString.Append($"{GetAccessibility(method)} Task {methodName}");
            }
            else
            {
                codeString.Append($"{GetAccessibility(method)} Task<{returnType}> {methodName}");
            }

            codeString.Append("(");//方法参数
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    codeString.Append(",");
                }
                codeString.Append($"{parameters[i].ToDisplayString()} {parameters[i].Name}");
            }
            codeString.AppendLine($")");

            codeString.AppendLine("{");//方法开始


            if (!method.ReturnsVoid)
            {
                new ValueTask( );
            }
            else
            {

            }
            codeString.AppendLine("}");
            return codeString.ToString();
        }
        public string GetPrecompile(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments) 
        {
            if (namedArguments.TryGetValue("Precompile",out TypedConstant typedConstant))
            {
                return typedConstant.Value?.ToString();
            }
            else if (this.m_namedArguments.TryGetValue("Precompile", out typedConstant))
            {
                return typedConstant.Value?.ToString();
            }
            return null;
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

            var methodName = GetMethodName(method, namedArguments);
            var returnType = GetReturnType(method, namedArguments);

            var parameters = method.Parameters;

            //生成开始
            var codeString = new StringBuilder();
            //以下生成异步
            codeString.AppendLine(GetComments(method));
         
            if (method.ReturnsVoid)
            {
                codeString.Append($"Task {methodName}");
            }
            else
            {
                codeString.Append($"Task<{returnType}> {methodName}");
            }

            codeString.Append("(");//方法参数
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    codeString.Append(",");
                }
                codeString.Append($"{parameters[i].ToDisplayString()} {parameters[i].Name}");
            }

            codeString.AppendLine($");");


            return codeString.ToString();
        }

        /// <summary>
        /// 查找所有方法
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IMethodSymbol> FindMethods()
        {
            return m_namedTypeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(a => !a.IsAsync);
        }
        private string GetClassName()
        {
            return m_namedTypeSymbol.Name;
        }

        private string GetComments(IMethodSymbol method)
        {
            return string.Empty;
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

        private string GetNamespace()
        {
            int r = this.m_namedTypeSymbol.ToDisplayString().LastIndexOf('.');
            if (r > 0)
            {
                return this.m_namedTypeSymbol.ToDisplayString().Substring(0, r);
            }
            return null;
        }

        private string GetRealTypeString(IParameterSymbol parameterSymbol)
        {
            switch (parameterSymbol.RefKind)
            {
                case RefKind.Ref:
                    return parameterSymbol.ToDisplayString().Replace("ref", string.Empty);

                case RefKind.Out:
                    return parameterSymbol.ToDisplayString().Replace("out", string.Empty);

                case RefKind.None:
                case RefKind.In:
                default:
                    return parameterSymbol.ToDisplayString();
            }
        }

        private string GetReturnType(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            return method.ReturnType.ToDisplayString();
        }

        private bool HasFlags(int value, int flag)
        {
            return (value & flag) == flag;
        }

        private bool HasReturn(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (method.ReturnsVoid || method.ReturnType.ToDisplayString() == typeof(Task).FullName)
            {
                return false;
            }
            return true;
        }
    }
}