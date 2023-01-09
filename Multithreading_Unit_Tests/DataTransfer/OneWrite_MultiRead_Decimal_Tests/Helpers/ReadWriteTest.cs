using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Multithreading_Library.DataTransfer;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers
{
    internal class ReadWriteTest
    {
        const Decimal VALUE1 = 1m;
        const Decimal VALUE2 = 10000000000m;
        private int Loopcount = 0;
        private int Msdelay = 0;
        volatile bool stop = false;
        OneWrite_MultiRead<decimal> decim = new OneWrite_MultiRead<decimal>(100);
        public Stopwatch Run(int loops, int msDelay, int threads)
        {
            this.Loopcount = loops;
            this.Msdelay = msDelay;
            List<Task> checkerTasks = new List<Task>();
            Stopwatch t = new Stopwatch();
            t.Start();
            Task setter = Task.Run((Action)Setter);
            for (int i = 0; i < threads; i++)
            {
                checkerTasks.Add(Task.Run((Action)Checker));
            }
            setter.Wait();
            t.Stop();
            stop = true;
            Task.WaitAll(checkerTasks.ToArray());
            foreach (Task checker in checkerTasks)
            {
                if (checker.IsFaulted)
                {
                    throw checker.Exception;
                }
            }
            return t;
        }
        void Setter()
        {
            for (int i = 0; i < Loopcount; i++)
            {
                if (Msdelay > 0)
                {
                    Task.Delay(Msdelay).Wait();
                }
                decim.Value = VALUE1;
                if (Msdelay > 0)
                {
                    Task.Delay(Msdelay).Wait();
                }
                decim.Value = VALUE2;
            }
        }

        void Checker()
        {
            while (true)
            {
                if (stop) return;
                var t = decim.Value;
                if (t == 0) continue;
                if (t != VALUE1 && t != VALUE2)
                {
                    throw new Exception("value is Thorn!");
                }
            }
        }
    }
}
