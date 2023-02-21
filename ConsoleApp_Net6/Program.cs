using IntelligentCoder;

namespace ConsoleApp_Net6
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            TestInterface testInterface = default;
        }
    }

    [AsyncMethodPoster]
    internal partial interface TestInterface
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        int Add(int a, int b);
    }
    //partial interface TestInterface
    //{
    //    void Add1(int a, int b);
    //}

}