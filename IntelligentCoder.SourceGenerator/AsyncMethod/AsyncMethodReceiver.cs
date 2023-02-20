using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IntelligentCoder.SourceGenerator
{
    /// <summary>
    /// RpcApi语法接收器
    /// </summary>
    sealed class AsyncMethodReceiver : ISyntaxReceiver
    {
        public const string GeneratorRpcProxyAttributeTypeName = "GeneratorRpcProxyAttribute";
        public const string RpcMethodAttributeTypeName = "GeneratorRpcMethodAttribute";

        /// <summary>
        /// 接口列表
        /// </summary>
        private readonly List<InterfaceDeclarationSyntax> interfaceSyntaxList = new List<InterfaceDeclarationSyntax>();

        /// <summary>
        /// 访问语法树 
        /// </summary>
        /// <param name="syntaxNode"></param>
        void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InterfaceDeclarationSyntax syntax)
            {
                interfaceSyntaxList.Add(syntax);
            }
        }

        public static INamedTypeSymbol GeneratorRpcProxyAttribute { get; private set; }

        /// <summary>
        /// 获取所有符号
        /// </summary>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public IEnumerable<INamedTypeSymbol> GetTypes(Compilation compilation)
        {
            //Debugger.Launch();
            GeneratorRpcProxyAttribute = compilation.GetTypeByMetadataName(GeneratorRpcProxyAttributeTypeName);
            if (GeneratorRpcProxyAttribute == null)
            {
                yield break;
            }
            foreach (var interfaceSyntax in interfaceSyntaxList)
            {
                var @interface = compilation.GetSemanticModel(interfaceSyntax.SyntaxTree).GetDeclaredSymbol(interfaceSyntax);
                if (@interface != null && IsRpcApiInterface(@interface))
                {
                    yield return @interface;
                }
            }
        }


        /// <summary>
        /// 是否为接口
        /// </summary>
        /// <param name="interface"></param>
        /// <returns></returns>
        public static bool IsRpcApiInterface(INamedTypeSymbol @interface)
        {
            if (GeneratorRpcProxyAttribute is null)
            {
                return false;
            }
            //Debugger.Launch();
            return @interface.GetAttributes().FirstOrDefault(a =>
            {
                if (a.AttributeClass.ToDisplayString() != GeneratorRpcProxyAttribute.ToDisplayString())
                {
                    return false;
                }
              
                return true;
            }) is not null;
        }


        /// <summary>
        /// 返回是否声明指定的特性
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute)
        {
            foreach (var attr in symbol.GetAttributes())
            {
                var attrClass = attr.AttributeClass;
                if (attrClass != null && attrClass.AllInterfaces.Contains(attribute))
                {
                    return true;
                }
            }
            return false;
        }
    }
}