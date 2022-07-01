using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers
{
    internal class Benchmark
    {
        object Decimal_Lock = new object();
        const Decimal VALUE1 = 1m;
        const Decimal VALUE2 = 10000000000m;
        Decimal d;
        private int Loopcount = 0;
        private int Msdelay = 0;
        volatile bool stop = false;
        public Stopwatch RunWithLock(int loops,  int threads, int msDelay)
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

                lock (Decimal_Lock)
                {
                    d = VALUE1;
                }
                if (Msdelay > 0)
                {
                    Task.Delay(Msdelay).Wait();
                }
                lock (Decimal_Lock)
                {
                    d = VALUE2;
                }
            }
        }

        void Checker()
        {
            while (true)
            {
                if (stop) return;
                lock (Decimal_Lock)
                {
                    var t = d;
                    if (t == 0) continue;
                    if (t != VALUE1 && t != VALUE2)
                    {
                        throw new Exception("value is Thorn!");
                    }
                }
            }
        }
    }
}
