using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace IntelligentCoder
{
    /// <summary>
    /// RpcApi语法接收器
    /// </summary>
    internal sealed class AsyncMethodReceiver : ISyntaxReceiver
    {
        public const string AsyncMethodPosterAttributeTypeName = "IntelligentCoder.AsyncMethodPosterAttribute";
        public const string AsyncMethodAttributeTypeName = "IntelligentCoder.AsyncMethodAttribute";

        public static INamedTypeSymbol GeneratorAttribute { get; private set; }

        public List<TypeDeclarationSyntax> TypeDeclarationSyntaxs { get; private set; } = new List<TypeDeclarationSyntax>();

        /// <summary>
        /// 是否为接口
        /// </summary>
        /// <param name="namedTypeSymbol"></param>
        /// <returns></returns>
        public static bool IsAsyncMethodPoster(INamedTypeSymbol namedTypeSymbol)
        {
            if (GeneratorAttribute is null)
            {
                return false;
            }
            //Debugger.Launch();
            return namedTypeSymbol.GetAttributes().FirstOrDefault(a =>
            {
                if (a.AttributeClass.ToDisplayString() != GeneratorAttribute.ToDisplayString())
                {
                    return false;
                }

                return true;
            }) is not null;
        }

        /// <summary>
        /// 获取所有符号
        /// </summary>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public IEnumerable<INamedTypeSymbol> GetAsyncMethodPosterTypes(Compilation compilation)
        {
            GeneratorAttribute = compilation.GetTypeByMetadataName(AsyncMethodPosterAttributeTypeName);
            if (GeneratorAttribute == null)
            {
                yield break;
            }

            foreach (var typeDeclarationSyntax in TypeDeclarationSyntaxs)
            {
                var namedTypeSymbol = compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree).GetDeclaredSymbol(typeDeclarationSyntax);
                if (namedTypeSymbol != null && IsAsyncMethodPoster(namedTypeSymbol))
                {
                    yield return namedTypeSymbol;
                }
            }
        }

        /// <summary>
        /// 访问语法树
        /// </summary>
        /// <param name="syntaxNode"></param>
        void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax symbol)
            {
                TypeDeclarationSyntaxs.Add(symbol);
            }
        }
    }
}