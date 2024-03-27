using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Multithreading_Library.DataTransfer;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers
{
    internal class ReadWriteTest
    {
        const Decimal Value1 = 1m;
        const Decimal Value2 = 10000000000m;
        private int _loopCount;
        private int _msDelay;
        private volatile bool _stop;
        readonly OneWrite_MultiRead<decimal> _decimal = new (1, true);
        public Stopwatch Run(int loops, int msDelay, int threads)
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
                _decimal.Value = Value1;
                if (_msDelay > 0)
                {
                    Task.Delay(_msDelay).Wait();
                }
                _decimal.Value = Value2;
            }
        }

        void Checker()
        {
            while (true)
            {
                if (_stop) return;
                var t = _decimal.Value;
                if (t == 0) continue;
                if (t != Value1 && t != Value2)
                {
                    throw new Exception("value is Thorn!");
                }
            }
        }
    }
}
