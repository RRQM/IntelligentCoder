using IntelligentCoder;
using TouchSocket.Core;

namespace ConsoleApp_Net6
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            var testClass1 = new TestClass1();
            testClass1.Add1();
            testClass1.Add1Async();
            testClass1.Add2();
            testClass1.Add2Async();
            testClass1.Add3();
            testClass1.MyAdd3Async();

            var testClass2 = new TestClass2();
            testClass2.Add(10, 20);

            var testInterfaceImp = new TestInterfaceImp();
            testInterfaceImp.Add2Async<Program>(10, 20);

            TestStaticClassExtension.AddAsync(10, 20);

            var path = "path";
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

        public void Add4(Span<byte> bytes)
        {

        }
    }
    #endregion

    #region 接口实现

    /// <inheritdoc cref = "System.IO.File.AppendAllLines(string, IEnumerable{string})"/>
    [AsyncMethodPoster]
    [AsyncMethodPoster(Precompile = "NET8")]
    public partial interface TestInterface
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [AsyncMethodIgnore]
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

    #region 泛型接口
    /// <summary>
    /// 缓存键值
    /// </summary>
    [IntelligentCoder.AsyncMethodPoster(Flags = IntelligentCoder.MemberFlags.Public)]
    public partial interface ICache<TKey, TValue> where TKey : class
    {
        /// <summary>
        /// 添加缓存。当缓存存在时，不会添加成功。
        /// </summary>
        /// <param name="entity">缓存实体</param>
        /// <exception cref="ArgumentNullException"></exception>
        bool AddCache(ICacheEntry<TKey, TValue> entity);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        /// <returns></returns>
        Task ClearCacheAsync();

        /// <summary>
        /// 判断缓存是否存在，且在生命周期内。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        bool ContainsCache(TKey key);

        /// <summary>
        /// 判断缓存是否存在，且在生命周期内。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<bool> ContainsCacheAsync(TKey key);

        /// <summary>
        /// 设置缓存，不管缓存存不存在，都会添加。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        bool SetCache(ICacheEntry<TKey, TValue> entity);

        /// <summary>
        /// 设置缓存，不管缓存存不存在，都会添加。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<bool> SetCacheAsync(ICacheEntry<TKey, TValue> entity);

        /// <summary>
        /// 获取指定键的缓存。
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        ICacheEntry<TKey, TValue> GetCache(TKey key);

        /// <summary>
        /// 获取指定键的缓存。
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<ICacheEntry<TKey, TValue>> GetCacheAsync(TKey key);

        /// <summary>
        /// 移除指定键的缓存。
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        bool RemoveCache(TKey key);

        /// <summary>
        /// 移除指定键的缓存。
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<bool> RemoveCacheAsync(TKey key);
    }

    public partial interface ICache<TKey, TValue>
    { }
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