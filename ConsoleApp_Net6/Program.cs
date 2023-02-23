using IntelligentCoder;

namespace ConsoleApp_Net6
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            TestClass1 testClass1 = new TestClass1();
            testClass1.Add1();
            testClass1.Add1Async();
            testClass1.Add2();
            testClass1.Add2Async();
            testClass1.Add3();
            testClass1.MyAdd3Async();


            TestInterfaceImp testInterfaceImp = new TestInterfaceImp();
            testInterfaceImp.Add2Async<int>(10,20);
        }
    }

    #region 常规类
    [AsyncMethodPoster]
    internal partial class TestClass1
    {
        public int Add1()
        {
            return 0;
        }

        public void Add2()
        {

        }

        [AsyncMethod(Template = "My{0}Async")]//测试模板生成
        public void Add3()
        {

        }
    }
    #endregion

    #region 接口实现
    #endregion

    [AsyncMethodPoster]
    public partial interface TestInterface
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        int Add(int a, int b);
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
        void Add2<T>(int a, int b);
    }

    [AsyncMethodPoster(Deep = 10)]
    public partial class TestInterfaceImp : TestInterface
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public void Add2<T>(int a, int b)
        {
            
        }
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