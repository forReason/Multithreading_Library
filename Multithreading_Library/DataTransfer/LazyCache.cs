namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// Defines a type and a target timespan to hold the value. 
    /// When the Timespan is expired, returns null or default.
    /// </summary>
    /// <remarks>
    /// This cache is lazy for performance reasons.
    /// It continues to hold the value until you clear it or dispose it. 
    /// It is very useful for caching values which have to be refreshed after a certain time.
    /// </remarks>
    /// <typeparam name="T">The type of the cache</typeparam>
    public class LazyCache<T> : IDisposable
    {
        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private T? _cacheValue;
        private DateTimeOffset _lastUse = DateTimeOffset.MinValue;
        /// <summary>
        /// Initializes a new instance of LazyCache with a specified duration.
        /// </summary>
        /// <param name="expirationTimespan">The timespan for which the value is valid</param>
        public LazyCache(TimeSpan expirationTimespan)
        {
            ExpirationTimespan = expirationTimespan;
        }
        /// <summary>
        /// Used to access or write the value.
        /// Returns default(T) when the cache is expired or not set.
        /// </summary>
        public T? Value
        {
            get
            {
                cacheLock.EnterReadLock();
                try
                {
                    if (_cacheValue == null || DateTimeOffset.Now - _lastUse > ExpirationTimespan)
                        return default;
                    return _cacheValue;
                }
                finally
                {
                    cacheLock.ExitReadLock();
                }
            }
            set
            {
                cacheLock.EnterWriteLock();
                try
                {
                    _cacheValue = value;
                    _lastUse = DateTimeOffset.Now;
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
        }
        /// <summary>
        /// The timespan for which the Value is valid.
        /// </summary>
        public TimeSpan ExpirationTimespan { get; set; }

        /// <summary>
        /// Clears the cache variable.
        /// </summary>
        public void Clear()
        {
            cacheLock.EnterWriteLock();
            try
            {
                _cacheValue = default;
                _lastUse = DateTimeOffset.MinValue;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Disposes of the cache.
        /// </summary>
        public void Dispose()
        {
            if (cacheLock != null)
            {
                cacheLock.Dispose();
                cacheLock = null!;
            }
        }
    }
}
