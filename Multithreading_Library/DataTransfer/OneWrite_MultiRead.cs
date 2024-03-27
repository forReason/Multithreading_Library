namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// This class is meant to push one single decimal from one thread to several threads
    /// </summary>
    public class OneWrite_MultiRead<T>
    {
        /// <summary>
        /// This class is meant to overcome the limitation of locking 
        /// when you have one thread that pushes values while multiple threads access this value.<Br/>
        /// </summary>
        /// <remarks>
        /// please note that deepCopy is strongly recommended for reference types such as arrays or custom classes.<br/>
        /// disable deepCopy for value times for better performance (double, bool, ...)
        /// <br/><br/>
        /// for optimal performance implement the ICloneable interface for your custom types with a .Clone() function.<br/>
        /// The following types are supported, although a reflection based fallback is implemented (slower):<br/>
        /// - Value Types<BR/>
        /// - IEnumerable (containing ICloneable types)
        /// - Custom types and type which implement the interface ICloneable
        /// </remarks>
        /// <param name="defaultValue"></param>
        /// <param name="deepCopy">specifies if input and output should be deep copied. This is important when handling reference types</param>
        public OneWrite_MultiRead(T defaultValue, bool deepCopy = true)
        {
            Value = defaultValue;
            DeepCopy = deepCopy;
        }

        /// <summary>
        /// holds an element with additional indicator of how many read threads are still active
        /// </summary>
        private class ElementWrapper
        {
            public T? Value;
            public volatile int ActiveCopies;
        }

        /// <summary>
        /// specifies if input and output should be deep copied. This is important when handling reference types
        /// </summary>
        /// <remarks>
        /// please note that deepCopy is strongly recommended for reference types such as arrays or custom classes.<br/>
        /// disable deepCopy for value times for better performance (double, bool, ...)
        /// <br/><br/>
        /// for optimal performance implement the ICloneable interface for your custom types with a .Clone() function.<br/>
        /// The following types are supported, although a reflection based fallback is implemented (slower):<br/>
        /// - Value Types<BR/>
        /// - IEnumerable (containing ICloneable types)
        /// - Custom types and type which implement the interface ICloneable
        /// </remarks>
        public bool DeepCopy { get; set; }
        private ElementWrapper _currentElement = new ElementWrapper();
        private volatile Queue<ElementWrapper> _boxes = new ();
        /// <summary>
        /// Reads or writes a new element. Please ensure to only access Set (value = something) from one singular thread to prevent condition issues
        /// </summary>
        public T? Value
        {
            get
            {
                // Increment the count of active copies
                Interlocked.Increment(ref _currentElement.ActiveCopies);

                // Perform the copy operation
                try
                {
                    T? copiedValue = DeepCopy ? DeepClone.Cloning.DeepClone(_currentElement.Value) : _currentElement.Value;
                    return copiedValue;
                }
                finally
                {
                    // Decrement the count of active copies
                    Interlocked.Decrement(ref _currentElement.ActiveCopies);
                }            
            }
            set => UpdateValue(value);
        }
        /// <summary>
        /// get or set the current value. 
        /// </summary>
        /// <remarks>
        /// NOTE: in case you want to hold on to the value longer, it is recommended to use Clone() instead! <br/>
        /// this is because the boxes are being re-used!
        /// </remarks>
        private void UpdateValue(T? value)
        {
            ElementWrapper newElement;
            if (_boxes.Count > 0 && _boxes.Peek().ActiveCopies == 0)
            {
                newElement = _boxes.Dequeue();
                newElement.Value = DeepCopy ? DeepClone.Cloning.DeepClone(value) : value;
            }
            else
            {
                newElement = new ElementWrapper { Value = DeepCopy ? DeepClone.Cloning.DeepClone(value) : value};
            }
            // Atomically update the _CurrentElement
            Interlocked.Exchange(ref _currentElement, newElement);
            _boxes.Enqueue(newElement);
        }
    }
}
