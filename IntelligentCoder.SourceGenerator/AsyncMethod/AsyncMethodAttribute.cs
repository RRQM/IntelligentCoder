using System;

namespace IntelligentCoder
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class AsyncMethodAttribute : Attribute
    {
        /// <summary>
        /// 预编译条件，当条件满足时才会被编译
        /// </summary>
        public string Precompile { get; set; }

        /// <summary>
        /// 生成异步方法名的模板，默认："{0}Async"。
        /// </summary>
        public string Template { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class AsyncMethodIgnoreAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    internal class AsyncMethodPosterAttribute : AsyncMethodAttribute
    {
        /// <summary>
        /// 当设置该值时，会直接按目标类型生成异步扩展方法。
        /// </summary>
        public Type Target { get; set; }

        /// <summary>
        /// 成员访问标识
        /// </summary>
        public MemberFlags Flags { get; set; }

        /// <summary>
        /// 忽略方法
        /// </summary>
        public string[] IgnoreMethods { get; set; }

        /// <summary>
        /// 检索继承深度，默认为0
        /// </summary>
        public int Deep { get; set; }
    }

    [Flags]
    internal enum MemberFlags
    {
        Public = 1,
        Protected = 2,
        Private = 4,
        Internal = 8
    }
}
