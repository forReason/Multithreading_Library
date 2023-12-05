using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// Represents a thread-safe, lazy-loading cache dictionary with expiration for each key.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public class LazyCacheDictionary<TKey, TValue> : IDisposable
    {
        private ConcurrentDictionary<TKey, (TValue Value, DateTimeOffset LastUse)> cache
            = new ConcurrentDictionary<TKey, (TValue, DateTimeOffset)>();
        private TimeSpan expirationTimespan;
        private Timer cleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyCacheDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="expirationTimespan">The timespan after which a value expires.</param>
        /// <param name="cleanupInterval">The interval at which the cache is checked for expired values. <br/>
        /// (take it rather long, probably > 5-10x expirationTimespan)</param>
        public LazyCacheDictionary(TimeSpan expirationTimespan, TimeSpan cleanupInterval)
        {
            this.expirationTimespan = expirationTimespan;
            cleanupTimer = new Timer(CleanupCache, null, cleanupInterval, cleanupInterval);
        }

        /// <summary>
        /// Retrieves the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value associated with the key if found and not expired; otherwise, the default value for the type of the value parameter.</returns>
        public TValue Get(TKey key)
        {
            if (cache.TryGetValue(key, out var cacheItem) && DateTimeOffset.Now - cacheItem.LastUse <= expirationTimespan)
            {
                return cacheItem.Value;
            }
            return default;
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// </summary>
        /// <param name="key">The key of the value to add or update.</param>
        /// <param name="value">The value to be associated with the key.</param>
        public void Set(TKey key, TValue value)
        {
            cache.AddOrUpdate(key, (value, DateTimeOffset.Now), (k, old) => (value, DateTimeOffset.Now));
        }

        /// <summary>
        /// Periodically cleans up expired cache entries.
        /// </summary>
        /// <param name="state">An object containing application-specific information relevant to the method invoked by this delegate, or null.</param>
        private void CleanupCache(object state)
        {
            var expiredKeys = cache.Where(kv => DateTimeOffset.Now - kv.Value.LastUse > expirationTimespan)
                                   .Select(kv => kv.Key)
                                   .ToList();

            foreach (var key in expiredKeys)
            {
                cache.TryRemove(key, out var _);
            }
        }

        /// <summary>
        /// Clears all the key-value pairs in the cache.
        /// </summary>
        public void Clear()
        {
            cache.Clear();
        }

        /// <summary>
        /// Releases the resources used by the <see cref="LazyCacheDictionary{TKey, TValue}"/>.
        /// </summary>
        public void Dispose()
        {
            cleanupTimer?.Dispose();
        }
    }
}