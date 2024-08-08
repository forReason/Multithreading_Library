using Multithreading_Library.DataTransfer;
using System;
using System.Diagnostics;
using Xunit;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests
{
    public class Test
    {

        [Fact]
        public void Test_Decimal_Push_Speed()
        {
            int loops = 200;
            int delay = 0;
            int readThreads = 8;
            Helpers.Benchmark benchmark = new Helpers.Benchmark();
            Stopwatch benchmarkWatch = benchmark.RunWithLock(loops,delay,readThreads);
            Helpers.ReadWriteTest test = new Helpers.ReadWriteTest();
            Stopwatch testWatch = test.Run(loops,delay,readThreads);
            if (testWatch.ElapsedTicks > benchmarkWatch.ElapsedTicks)
            {
                throw new Exception("method was slower than test!");
            }
        }
        [Fact]
        public void Test_Decimal_Push_Accuracy()
        {
            OneWrite_MultiRead<decimal> _decimal = new OneWrite_MultiRead<decimal>(0, false);
            for (decimal i = 0; i < 100000; i++)
            {
                _decimal.Value = i;
                decimal test = _decimal.Value;
                if (test != i)
                {
                    throw new Exception("value incorrect!!!");
                }
            }
        }

        
    }
}
