using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Linq;

namespace IntelligentCoder
{
    /// <summary>
    /// AsyncMethod代码生成器
    /// </summary>
    [Generator]
    public class AsyncMethodSourceGenerator : ISourceGenerator
    {
        readonly string AsyncMethodAttribute = @"

using System;
using System.Collections.Generic;
using System.Text;

namespace IntelligentCoder
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    class AsyncMethodAttribute : Attribute
    {
        /// <summary>
        /// 是否使用ValueTask包装
        /// </summary>
        public bool ValueTask { get; set; }

        /// <summary>
        /// 预编译条件，当条件满足时才会被编译
        /// </summary>
        public string Precompile { get; set; }

        /// <summary>
        /// 当设置该值时，会直接按目标类型生成异步方法。
        /// </summary>
        public Type Target { get; set; }
    }
}

";
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) => i.AddSource("AsyncMethodAttribute.g.cs", AsyncMethodAttribute));

            context.RegisterForSyntaxNotifications(() => new AsyncMethodReceiver());
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="context"></param>
        public void Execute(GeneratorExecutionContext context)
        {
            var s = context.Compilation.GetMetadataReference(context.Compilation.Assembly);

            if (context.SyntaxReceiver is AsyncMethodReceiver receiver)
            {
                Debugger.Launch();
                var builders = receiver
                    .GetAsyncMethodPosterTypes(context.Compilation)
                    .Select(i => new AsyncMethodCodeBuilder(i))
                    .Distinct();
                //Debugger.Launch();
                foreach (var builder in builders)
                {
                    context.AddSource($"{builder.GetFileName()}.g.cs", builder.ToSourceText());
                }
            }
        }
    }
}
