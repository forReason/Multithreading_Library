using Multithreading_Library.DataTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests
{
    public class Test
    {
        [Fact]
        public void Test_Decimal_Push_Speed()
        {
            int loops = 2000;
            int delay = 0;
            int readThreads = 8;
            Helpers.Benchmark benchmark = new Helpers.Benchmark();
            Stopwatch benchmark_watch = benchmark.RunWithLock(loops,delay,readThreads);
            Helpers.ReadWriteTest test = new Helpers.ReadWriteTest();
            Stopwatch test_watch = test.Run(loops,delay,readThreads);
            if (test_watch.ElapsedTicks > benchmark_watch.ElapsedTicks)
            {
                throw new Exception("method was slower than test!");
            }
        }
        [Fact]
        public void Test_Decimal_Push_Accuracy()
        {
            OneWrite_MultiRead_Decimal decim = new OneWrite_MultiRead_Decimal();
            for (decimal i = 0; i < 100000; i++)
            {
                decim.Value = i;
                decimal test = decim.Value;
                if (test != i)
                {
                    throw new Exception("value incorrect!!!");
                }
            }
        }
    }
}
