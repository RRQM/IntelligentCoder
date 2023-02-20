﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentCoder
{
    /// <summary>
    /// RpcApi代码构建器
    /// </summary>
    internal sealed class AsyncMethodCodeBuilder
    {
        /// <summary>
        /// 接口符号
        /// </summary>
        private readonly INamedTypeSymbol m_rpcApi;

        private readonly Dictionary<string, TypedConstant> m_rpcApiNamedArguments;

        /// <summary>
        /// RpcApi代码构建器
        /// </summary>
        /// <param name="rpcApi"></param>
        public AsyncMethodCodeBuilder(INamedTypeSymbol rpcApi)
        {
            m_rpcApi = rpcApi;
            AttributeData attributeData = rpcApi.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.GeneratorAttributeTypeName);

            m_rpcApiNamedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);

            if (m_rpcApiNamedArguments.TryGetValue("Prefix", out var typedConstant))
            {
                Prefix = typedConstant.Value.ToString();
            }
            else
            {
                Prefix = m_rpcApi.ToDisplayString();
            }
        }

        public string Prefix { get; set; }

        public string ServerName { get; set; }

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

        public string GetFileName()
        {
            return m_rpcApi.ToDisplayString() + "Generator";
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
            var builder = new StringBuilder();
            foreach (var item in Usings)
            {
                builder.AppendLine(item);
            }
            builder.AppendLine($"namespace {GetNamespace()}");
            builder.AppendLine("{");

            if (AllowAsync(CodeGeneratorFlag.InterfaceSync) || AllowAsync(CodeGeneratorFlag.InterfaceAsync))
            {
                BuildIntereface(builder);
            }

            if (AllowAsync(CodeGeneratorFlag.ExtensionSync) || AllowAsync(CodeGeneratorFlag.ExtensionAsync))
            {
                BuildMethod(builder);
            }
            builder.AppendLine("}");

            // System.Diagnostics.Debugger.Launch();
            return builder.ToString();
        }

        /// <summary>
        /// 查找接口类型及其继承的接口的所有方法
        /// </summary>
        /// <param name="httpApi">接口</param>
        /// <returns></returns>
        private IEnumerable<IMethodSymbol> FindApiMethods()
        {
            return m_rpcApi
                .GetMembers()
                .OfType<IMethodSymbol>();
        }

        private bool AllowAsync(CodeGeneratorFlag flag, IMethodSymbol method = default, Dictionary<string, TypedConstant> namedArguments = default)
        {
            if (method != null && method.Name.EndsWith("Async"))
            {
                return true;
            }
            if (namedArguments != null && namedArguments.TryGetValue("GeneratorFlag", out var typedConstant))
            {
                return ((CodeGeneratorFlag)typedConstant.Value).HasFlag(flag);
            }
            else if (m_rpcApiNamedArguments != null && m_rpcApiNamedArguments.TryGetValue("GeneratorFlag", out typedConstant))
            {
                return ((CodeGeneratorFlag)typedConstant.Value).HasFlag(flag);
            }
            return true;
        }

        private bool AllowSync(CodeGeneratorFlag flag, IMethodSymbol method = default, Dictionary<string, TypedConstant> namedArguments = default)
        {
            if (method != null && method.Name.EndsWith("Async"))
            {
                return false;
            }
            if (namedArguments != null && namedArguments.TryGetValue("GeneratorFlag", out var typedConstant))
            {
                return ((CodeGeneratorFlag)typedConstant.Value).HasFlag(flag);
            }
            else if (m_rpcApiNamedArguments != null && m_rpcApiNamedArguments.TryGetValue("GeneratorFlag", out typedConstant))
            {
                return ((CodeGeneratorFlag)typedConstant.Value).HasFlag(flag);
            }
            return true;
        }

        private bool IsInheritedInterface()
        {
            if (m_rpcApiNamedArguments.TryGetValue("InheritedInterface", out var typedConstant))
            {
                return typedConstant.Value is bool value && value;
            }
            return true;
        }


        private void BuildIntereface(StringBuilder builder)
        {
            var interfaceNames = new List<string>();
            if (IsInheritedInterface())
            {
                var interfaceNames1 = m_rpcApi.Interfaces
               .Where(a => AsyncMethodReceiver.IsRpcApiInterface(a))
               .Select(a => $"I{new AsyncMethodCodeBuilder(a).GetClassName()}");

                var interfaceNames2 = m_rpcApi.Interfaces
                   .Where(a => !AsyncMethodReceiver.IsRpcApiInterface(a))
                   .Select(a => a.ToDisplayString());

                interfaceNames.AddRange(interfaceNames1);
                interfaceNames.AddRange(interfaceNames2);
            }

            if (interfaceNames.Count == 0)
            {
                builder.AppendLine($"public interface I{GetClassName()}");
            }
            else
            {
                builder.AppendLine($"public interface I{GetClassName()} :{string.Join(",", interfaceNames)}");
            }

            builder.AppendLine("{");
            //Debugger.Launch();

            foreach (var method in FindApiMethods())
            {
                var methodCode = BuildMethodInterface(method);
                builder.AppendLine(methodCode);
            }

            builder.AppendLine("}");
        }

        private void BuildMethod(StringBuilder builder)
        {
            builder.AppendLine($"public static class {GetClassName()}Extensions");
            builder.AppendLine("{");
            //Debugger.Launch();

            foreach (var method in FindApiMethods())
            {
                var methodCode = BuildMethod(method);
                builder.AppendLine(methodCode);
            }

            builder.AppendLine("}");
        }

        private string BuildMethod(IMethodSymbol method)
        {
            //Debugger.Launch();
            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.RpcMethodAttributeTypeName);
            if (attributeData is null)
            {
                return string.Empty;
            }

            //Debugger.Launch();

            var namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);

            var invokeKey = GetInvokeKey(method, namedArguments);
            var methodName = GetMethodName(method, namedArguments);
            var genericConstraintTypes = GetGenericConstraintTypes(method, namedArguments);
            var isIncludeCallContext = IsIncludeCallContext(method, namedArguments);
            var allowSync = AllowSync(CodeGeneratorFlag.ExtensionSync, method, namedArguments);
            var allowAsync = AllowAsync(CodeGeneratorFlag.ExtensionAsync, method, namedArguments);
            var returnType = GetReturnType(method, namedArguments);

            var parameters = method.Parameters;
            if (isIncludeCallContext)
            {
                parameters = parameters.RemoveAt(0);
            }

            //生成开始
            var codeString = new StringBuilder();
            bool isOut = false;
            bool isRef = false;

            if (allowSync)
            {
                codeString.AppendLine("///<summary>");
                codeString.AppendLine($"///{GetDescription(method)}");
                codeString.AppendLine("///</summary>");
                codeString.Append($"public static {returnType} {methodName}");
                codeString.Append("<TClient>(");//方法参数

                codeString.Append($"this TClient client");

                codeString.Append(",");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        codeString.Append(",");
                    }

                    codeString.Append($"{parameters[i].ToDisplayString()} {parameters[i].Name}");
                }
                if (parameters.Length > 0)
                {
                    codeString.Append(",");
                }
                codeString.Append("IInvokeOption invokeOption = default");
                codeString.AppendLine($") where TClient:{string.Join(",", genericConstraintTypes)}");

                codeString.AppendLine("{");//方法开始

                codeString.AppendLine("if (client.TryCanInvoke?.Invoke(client)==false)");
                codeString.AppendLine("{");
                codeString.AppendLine("throw new RpcException(\"Rpc无法执行。\");");
                codeString.AppendLine("}");

                if (parameters.Length > 0)
                {
                    codeString.Append($"object[] parameters = new object[]");
                    codeString.Append("{");

                    foreach (var parameter in parameters)
                    {
                        if (parameter.RefKind == RefKind.Ref)
                        {
                            isRef = true;
                        }
                        if (parameter.RefKind == RefKind.Out)
                        {
                            isOut = true;
                            codeString.Append($"default({GetRealTypeString(parameter)})");
                        }
                        else
                        {
                            codeString.Append(parameter.Name);
                        }
                        if (!parameter.Equals(parameters[parameters.Length - 1], SymbolEqualityComparer.Default))
                        {
                            codeString.Append(",");
                        }
                    }
                    codeString.AppendLine("};");

                    if (isOut || isRef)
                    {
                        codeString.Append($"Type[] types = new Type[]");
                        codeString.Append("{");
                        foreach (var parameter in parameters)
                        {
                            codeString.Append($"typeof({GetRealTypeString(parameter)})");
                            if (!parameter.Equals(parameters[parameters.Length - 1], SymbolEqualityComparer.Default))
                            {
                                codeString.Append(",");
                            }
                        }
                        codeString.AppendLine("};");
                    }
                }

                if (!method.ReturnsVoid)
                {
                    if (parameters.Length == 0)
                    {
                        codeString.Append(string.Format("{0} returnData=client.Invoke<{0}>", returnType));
                        codeString.Append("(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, null);");
                    }
                    else if (isOut || isRef)
                    {
                        codeString.Append(string.Format("{0} returnData=client.Invoke<{0}>", returnType));
                        codeString.Append("(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption,ref parameters,types);");
                    }
                    else
                    {
                        codeString.Append(string.Format("{0} returnData=client.Invoke<{0}>", returnType));
                        codeString.Append("(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, parameters);");
                    }
                }
                else
                {
                    if (parameters.Length == 0)
                    {
                        codeString.Append("client.Invoke(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, null);");
                    }
                    else if (isOut || isRef)
                    {
                        codeString.Append("client.Invoke(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption,ref parameters,types);");
                    }
                    else
                    {
                        codeString.Append("client.Invoke(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, parameters);");
                    }
                }
                //Debugger.Launch();
                if (isOut || isRef)
                {
                    codeString.AppendLine("if(parameters!=null)");
                    codeString.AppendLine("{");
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        codeString.AppendLine(string.Format("{0}=({1})parameters[{2}];", parameters[i].Name, GetRealTypeString(parameters[i]), i));
                    }
                    codeString.AppendLine("}");
                    if (isOut)
                    {
                        codeString.AppendLine("else");
                        codeString.AppendLine("{");
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].RefKind == RefKind.Out)
                            {
                                codeString.AppendLine(string.Format("{0}=default({1});", parameters[i].Name, GetRealTypeString(parameters[i])));
                            }
                        }
                        codeString.AppendLine("}");
                    }
                }

                if (!method.ReturnsVoid)
                {
                    codeString.AppendLine("return returnData;");
                }

                codeString.AppendLine("}");
            }

            if (isOut || isRef)
            {
                return codeString.ToString();
            }

            if (allowAsync)
            {
                //以下生成异步
                codeString.AppendLine("///<summary>");
                codeString.AppendLine($"///{GetDescription(method)}");
                codeString.AppendLine("///</summary>");
                if (method.ReturnsVoid)
                {
                    codeString.Append($"public static Task {methodName}Async");
                }
                else
                {
                    codeString.Append($"public static Task<{returnType}> {methodName}Async");
                }

                codeString.Append("<TClient>(");//方法参数

                codeString.Append($"this TClient client");

                codeString.Append(",");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        codeString.Append(",");
                    }
                    codeString.Append($"{parameters[i].ToDisplayString()} {parameters[i].Name}");
                }
                if (parameters.Length > 0)
                {
                    codeString.Append(",");
                }
                codeString.Append("IInvokeOption invokeOption = default");
                codeString.AppendLine($") where TClient:{string.Join(",", genericConstraintTypes)}");

                codeString.AppendLine("{");//方法开始

                codeString.AppendLine("if (client.TryCanInvoke?.Invoke(client)==false)");
                codeString.AppendLine("{");
                codeString.AppendLine($"throw new RpcException(\"Rpc无法执行。\");");
                codeString.AppendLine("}");

                if (parameters.Length > 0)
                {
                    codeString.Append($"object[] parameters = new object[]");
                    codeString.Append("{");
                    foreach (var parameter in parameters)
                    {
                        codeString.Append(parameter.Name);
                        if (!parameter.Equals(parameters[parameters.Length - 1], SymbolEqualityComparer.Default))
                        {
                            codeString.Append(",");
                        }
                    }
                    codeString.AppendLine("};");
                }

                if (!method.ReturnsVoid)
                {
                    if (parameters.Length == 0)
                    {
                        codeString.Append(string.Format("return client.InvokeAsync<{0}>", returnType));
                        codeString.Append("(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, null);");
                    }
                    else
                    {
                        codeString.Append(string.Format("return client.InvokeAsync<{0}>", returnType));
                        codeString.Append("(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, parameters);");
                    }
                }
                else
                {
                    if (parameters.Length == 0)
                    {
                        codeString.Append("return client.InvokeAsync(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, null);");
                    }
                    else
                    {
                        codeString.Append("return client.InvokeAsync(");
                        codeString.Append($"\"{invokeKey}\"");
                        codeString.AppendLine(",invokeOption, parameters);");
                    }
                }
                codeString.AppendLine("}");
            }

            return codeString.ToString();
        }

        private string BuildMethodInterface(IMethodSymbol method)
        {
            //Debugger.Launch();
            var attributeData = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == AsyncMethodReceiver.RpcMethodAttributeTypeName);
            if (attributeData is null)
            {
                return string.Empty;
            }

            //Debugger.Launch();

            var namedArguments = attributeData.NamedArguments.ToDictionary(a => a.Key, a => a.Value);

            var invokeKey = GetInvokeKey(method, namedArguments);
            var methodName = GetMethodName(method, namedArguments);
            var genericConstraintTypes = GetGenericConstraintTypes(method, namedArguments);
            var isIncludeCallContext = IsIncludeCallContext(method, namedArguments);
            var allowSync = AllowSync(CodeGeneratorFlag.InterfaceSync, method, namedArguments);
            var allowAsync = AllowAsync(CodeGeneratorFlag.InterfaceAsync, method, namedArguments);
            var returnType = GetReturnType(method, namedArguments);

            var parameters = method.Parameters;
            if (isIncludeCallContext)
            {
                parameters = parameters.RemoveAt(0);
            }

            //生成开始
            var codeString = new StringBuilder();
            bool isOut = false;
            bool isRef = false;

            if (allowSync)
            {
                codeString.AppendLine("///<summary>");
                codeString.AppendLine($"///{GetDescription(method)}");
                codeString.AppendLine("///</summary>");
                codeString.Append($"{returnType} {methodName}");
                codeString.Append("(");//方法参数
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        codeString.Append(",");
                    }

                    codeString.Append($"{parameters[i].ToDisplayString()} {parameters[i].Name}");
                }
                if (parameters.Length > 0)
                {
                    codeString.Append(",");
                }
                codeString.Append("IInvokeOption invokeOption = default");
                codeString.AppendLine($");");
            }

            if (isOut || isRef)
            {
                return codeString.ToString();
            }

            if (allowAsync)
            {
                //以下生成异步
                codeString.AppendLine("///<summary>");
                codeString.AppendLine($"///{GetDescription(method)}");
                codeString.AppendLine("///</summary>");
                if (method.ReturnsVoid)
                {
                    codeString.Append($"Task {methodName}Async");
                }
                else
                {
                    codeString.Append($"Task<{returnType}> {methodName}Async");
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
                if (parameters.Length > 0)
                {
                    codeString.Append(",");
                }
                codeString.Append("IInvokeOption invokeOption = default");
                codeString.AppendLine($");");
            }

            return codeString.ToString();
        }

        private string GetClassName()
        {
            if (m_rpcApiNamedArguments.TryGetValue("ClassName", out var typedConstant))
            {
                return typedConstant.Value?.ToString();
            }
            else if (m_rpcApi.Name.StartsWith("I"))
            {
                return m_rpcApi.Name.Remove(0, 1);
            }
            return m_rpcApi.Name;
        }

        private string GetDescription(IMethodSymbol method)
        {
            var desattribute = method.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == typeof(DescriptionAttribute).FullName);
            if (desattribute is null || desattribute.ConstructorArguments.Length == 0)
            {
                return "无注释信息";
            }

            return desattribute.ConstructorArguments[0].Value?.ToString();
        }

        private string[] GetGenericConstraintTypes(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (namedArguments.TryGetValue("GenericConstraintTypes", out var typedConstant))
            {
                return typedConstant.Values.Sort((a, b) =>
                {
                    if (a.Type.IsAbstract)
                    {
                        return 1;
                    }
                    return -1;
                }).Select(a => a.Value.ToString()).ToArray();
            }
            else if (m_rpcApiNamedArguments.TryGetValue("GenericConstraintTypes", out typedConstant))
            {
                return typedConstant.Values.Sort((a, b) =>
                {
                    if (a.Type.IsAbstract)
                    {
                        return 1;
                    }
                    return -1;
                }).Select(a => a.Value.ToString()).ToArray();
            }
            else
            {
                return new string[] { "TouchSocket.Rpc.IRpcClient" };
            }
        }

        private string GetInvokeKey(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (namedArguments.TryGetValue("InvokeKey", out var typedConstant))
            {
                return typedConstant.Value?.ToString() ?? string.Empty;
            }
            else if (namedArguments.TryGetValue("MethodInvoke", out typedConstant) && typedConstant.Value is bool b && b)
            {
                return GetMethodName(method, namedArguments);
            }
            else if (m_rpcApiNamedArguments.TryGetValue("MethodInvoke", out typedConstant) && typedConstant.Value is bool c && c)
            {
                return GetMethodName(method, namedArguments);
            }
            else
            {
                return $"{Prefix}.{method.Name}".ToLower();
            }
        }

        private string GetMethodName(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (namedArguments.TryGetValue("MethodName", out var typedConstant))
            {
                return typedConstant.Value?.ToString() ?? string.Empty;
            }

            return method.Name.EndsWith("Async") ? method.Name.Replace("Async", string.Empty) : method.Name;
        }

        private string GetNamespace()
        {
            if (m_rpcApiNamedArguments.TryGetValue("Namespace", out var typedConstant))
            {
                return typedConstant.Value?.ToString() ?? "TouchSocket.Rpc.Generators";
            }
            return "TouchSocket.Rpc.Generators";
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
            if (method.ReturnType.ToDisplayString().Contains(typeof(Task).FullName))
            {
                string methodname = method.ReturnType.ToDisplayString().Trim().Replace($"{typeof(Task).FullName}<", string.Empty);
                methodname = methodname.Remove(methodname.Length - 1);
                return methodname;
            }
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

        private bool IsIncludeCallContext(IMethodSymbol method, Dictionary<string, TypedConstant> namedArguments)
        {
            if (namedArguments.TryGetValue("MethodFlags", out var typedConstant))
            {
                return typedConstant.Value is int value && HasFlags(value, 2);
            }
            else if (m_rpcApiNamedArguments.TryGetValue("MethodFlags", out typedConstant))
            {
                return typedConstant.Value is int value && HasFlags(value, 2);
            }
            return false;
        }
    }
}