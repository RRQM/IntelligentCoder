using IntelligentCoder;

namespace ConsoleApp_Net6
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            TestInterface testInterface = default;
            testInterface.AddAsync(10, 10, 10);

            TestClass2 testClass2 = new TestClass2();
            testClass2.Add(10, 10);

            TestStaticClassExtension.AddAsync(10,20);
        }
    }

    [AsyncMethodPoster(IgnoreMethods = new string[] { "Add2" })]
    public partial interface TestInterface
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        void Add<T>(int a, int b, T c);
    }

    public partial interface TestInterface
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        void Add2<T>(int a, int b, T c);
    }

    [AsyncMethodPoster]
    static partial class TestClass
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }
    }

    public class TestClass2
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Add2(int a, int b)
        {
            return a + b;
        }
    }

    [AsyncMethodPoster(Target = typeof(TestInterface))]
    public static partial class TestClass2Extension
    {

    }

    [AsyncMethodPoster(Target = typeof(TestStaticClass))]
    public static partial class TestStaticClassExtension
    {

    }

    public static class TestStaticClass
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }
    }
}