using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers
{
    internal class Benchmark
    {
        readonly object _decimalLock = new ();
        const Decimal Value1 = 1m;
        const Decimal Value2 = 10000000000m;
        Decimal _d;
        private int _loopCount;
        private int _msDelay;
        volatile bool _stop;
        public Stopwatch RunWithLock(int loops,  int threads, int msDelay)
        {
            _loopCount = loops;
            _msDelay = msDelay;
            List<Task> checkerTasks = new List<Task>();
            Stopwatch t = new Stopwatch();
            t.Start();
            Task setter = Task.Run(Setter);
            
            for (int i = 0; i < threads; i++)
            {
                checkerTasks.Add(Task.Run(Checker));
            }
            setter.Wait();
            t.Stop();
            _stop = true;
            Task.WaitAll(checkerTasks.ToArray());
            foreach (Task checker in checkerTasks)
            {
                if (checker.IsFaulted)
                {
                    throw checker.Exception!;
                }
            }
            return t;
        }
        void Setter()
        {
            for (int i = 0; i < _loopCount; i++)
            {
                if (_msDelay > 0)
                {
                    Task.Delay(_msDelay).Wait();
                }

                lock (_decimalLock)
                {
                    _d = Value1;
                }
                if (_msDelay > 0)
                {
                    Task.Delay(_msDelay).Wait();
                }
                lock (_decimalLock)
                {
                    _d = Value2;
                }
            }
        }

        void Checker()
        {
            while (true)
            {
                if (_stop) return;
                lock (_decimalLock)
                {
                    var t = _d;
                    if (t == 0) continue;
                    if (t != Value1 && t != Value2)
                    {
                        throw new Exception("value is Thorn!");
                    }
                }
            }
        }
    }
}
