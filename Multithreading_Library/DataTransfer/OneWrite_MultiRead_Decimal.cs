using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// This class is ment to push one single decimal from one thread to several threads
    /// </summary>
    public class OneWrite_MultiRead_Decimal
    {
        public OneWrite_MultiRead_Decimal(int msBeforeDataboxReuse = 1000)
        {
            MsBeforeDataboxReuse = msBeforeDataboxReuse;
        }
        public int MsBeforeDataboxReuse { get; set; }
        public decimal Value { get { return _CurrentBox.Value; } set { UpdateValue(value); } }
        private volatile StrongBox<decimal> _CurrentBox = new StrongBox<decimal>(0);
        private Queue<DateTime> DateTimes = new Queue<DateTime>();
        volatile Queue<StrongBox<decimal>> Boxes = new Queue<StrongBox<decimal>>();
        private void UpdateValue(decimal value)
        {
            StrongBox<decimal> box;
            if (DateTimes.Count > 2 && DateTime.Now > DateTimes.First().AddMilliseconds(MsBeforeDataboxReuse))
            {
                box = Boxes.Dequeue();
                DateTimes.Dequeue();
            }
            else
            {
                box = new StrongBox<decimal>();
            }
            box.Value = value;
            _CurrentBox = box;
            Boxes.Enqueue(box);
            DateTimes.Enqueue(DateTime.Now);
        }
    }
}
