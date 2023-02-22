using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntelligentCoder;

namespace ConsoleApp_Net6
{
    internal partial class Program
    {
        public void TestClass()
        {
            TestClass1 testClass1 = new TestClass1();
            testClass1.Add1Async();
            testClass1.Add2Async();
        }
    }

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
    }
}
