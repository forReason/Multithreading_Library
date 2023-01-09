using System.Runtime.CompilerServices;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// This class is ment to push one single decimal from one thread to several threads
    /// </summary>
    public class OneWrite_MultiRead<T>
    {
        public OneWrite_MultiRead(T defaultValue, int msBeforeDataboxReuse = 1000)
        {
            MsBeforeDataboxReuse = msBeforeDataboxReuse;
            Value = defaultValue;
        }
        public int MsBeforeDataboxReuse { get; set; }
        /// <summary>
        /// get or set the current value. 
        /// </summary>
        /// <remarks>
        /// NOTE: in case you want to hold on to the value longer, it is recommended to use Clone() instead! </br>
        /// this is because the boxes are beeing re-used!
        /// </remarks>
        public T Value { get { return _CurrentBox.Value; } set { UpdateValue(value); } }
        private volatile StrongBox<T> _CurrentBox = new StrongBox<T>();
        private Queue<DateTime> DateTimes = new Queue<DateTime>();
        volatile Queue<StrongBox<T>> Boxes = new Queue<StrongBox<T>>();
        private void UpdateValue(T value)
        {
            StrongBox<T> box;
            if (DateTimes.Count > 2 && DateTime.Now > DateTimes.First().AddMilliseconds(MsBeforeDataboxReuse))
            {
                box = Boxes.Dequeue();
                DateTimes.Dequeue();
            }
            else
            {
                box = new StrongBox<T>();
            }
            box.Value = value;
            _CurrentBox = box;
            Boxes.Enqueue(box);
            DateTimes.Enqueue(DateTime.Now);
        }
        public T Clone()
        {
            T newObject = (T)Activator.CreateInstance(Value.GetType());

            foreach (var originalProp in Value.GetType().GetProperties())
            {
                originalProp.SetValue(newObject, originalProp.GetValue(Value));
            }

            return newObject;
        }
    }
}
