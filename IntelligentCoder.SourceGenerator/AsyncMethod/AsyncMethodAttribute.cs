using System;
using System.Collections.Generic;
using System.Text;

namespace IntelligentCoder
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class AsyncMethodAttribute : Attribute
    {
        /// <summary>
        /// 是否使用ValueTask包装
        /// </summary>
        public bool ValueTask { get; set; }

        /// <summary>
        /// 预编译条件，当条件满足时才会被编译
        /// </summary>
        public string Precompile { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    internal class AsyncMethodPoster : AsyncMethodAttribute
    {
        /// <summary>
        /// 当设置该值时，会直接按目标类型生成异步方法。
        /// </summary>
        public Type Target { get; set; }

        /// <summary>
        /// 忽略方法
        /// </summary>
        public string IgnoreMethods { get; set; }
    }
}
