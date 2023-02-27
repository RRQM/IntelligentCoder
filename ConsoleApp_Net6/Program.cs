using IntelligentCoder;

namespace ConsoleApp_Net6
{
    public partial class Program
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

            TestClass2 testClass2 = new TestClass2();
            testClass2.Add(10,20);

            TestInterfaceImp testInterfaceImp = new TestInterfaceImp();
            testInterfaceImp.Add2Async<Program>(10, 20);

            TestStaticClassExtension.AddAsync(10, 20);

            string path = "path";
            FileAsync.CreateAsync(path);
            FileAsync.OpenAsync(path, FileMode.Open);
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

    /// <inheritdoc cref = "System.IO.File.AppendAllLines(string, IEnumerable{string})"/>
    [AsyncMethodPoster]
    [AsyncMethodPoster(Precompile ="NET8")]
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
        void Add2<T>(int a, int b) where T : class;
    }

    [AsyncMethodPoster(Deep = 10)]
    public partial class TestInterfaceImp : TestInterface
    {
        /// <summary>
        /// <inheritdoc cref="TestInterface.Add(int, int)"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int Add(int a, int b)
        {
            return a + b;
        }

        /// <summary>
        /// asdsa
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void Add2<T>(int a, int b) where T : class
        {

        }
    }
    #endregion

    #region 常规类扩展
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

    [AsyncMethodPoster(Target = typeof(TestClass2))]
    public static partial class TestClass2Extension
    {

    }
    #endregion

    #region 静态类扩展
    [AsyncMethodPoster(Target = typeof(TestStaticClass))]
    internal static partial class TestStaticClassExtension
    {

    }

    public static class TestStaticClass
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }
    }
    #endregion

    [AsyncMethodPoster(Target = typeof(System.IO.File))]
    static partial class FileAsync
    {

    }
}