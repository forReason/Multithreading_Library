namespace Multithreading_Library.DataTransfer
{
    internal struct CacheDateValue<T>
    {
        internal T Value;
        internal DateTime Time;
        public int ActiveCopies;
    }
}
