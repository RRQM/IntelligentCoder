using Microsoft.CodeAnalysis;
using System.Linq;

namespace IntelligentCoder.SourceGenerator
{
    /// <summary>
    /// HttpApi代码生成器
    /// </summary>
    [Generator]
    public class AsyncMethodSourceGenerator : ISourceGenerator
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
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
                var builders = receiver
                    .GetTypes(context.Compilation)
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
