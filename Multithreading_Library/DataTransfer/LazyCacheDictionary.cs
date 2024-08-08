using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// Represents a thread-safe, lazy-loading cache dictionary with expiration for each key.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public class LazyCacheDictionary<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, (TValue Value, DateTimeOffset LastUse)> _cache = new ();
        private readonly TimeSpan _expirationTimespan;
        private readonly Timer _cleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyCacheDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="expirationTimespan">The timespan after which a value expires.</param>
        /// <param name="cleanupInterval">The interval at which the cache is checked for expired values. <br/>
        /// (take it rather long, probably > 5-10x expirationTimespan)</param>
        public LazyCacheDictionary(TimeSpan expirationTimespan, TimeSpan cleanupInterval)
        {
            this._expirationTimespan = expirationTimespan;
            _cleanupTimer = new Timer(CleanupCache!, null, cleanupInterval, cleanupInterval);
        }

        /// <summary>
        /// Retrieves the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value associated with the key if found and not expired; otherwise, the default value for the type of the value parameter.</returns>
        public TValue? Get(TKey key)
        {
            if (_cache.TryGetValue(key, out var cacheItem) && DateTimeOffset.Now - cacheItem.LastUse <= _expirationTimespan)
            {
                return cacheItem.Value;
            }
            return default;
        }
        /// <summary>
        /// attempts to retrieve the value.
        /// </summary>
        /// <param name="key">the key to look up</param>
        /// <param name="value">the value to return</param>
        /// <returns>true or false based on if the action was successful</returns>
        public bool TryGet(TKey key, out TValue? value)
        {
            if (_cache.TryGetValue(key, out var cacheItem) && DateTimeOffset.Now - cacheItem.LastUse <= _expirationTimespan)
            {
                value = cacheItem.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// </summary>
        /// <param name="key">The key of the value to add or update.</param>
        /// <param name="value">The value to be associated with the key.</param>
        public void Set(TKey key, TValue value)
        {
            _cache.AddOrUpdate(key, (value, DateTimeOffset.Now), (_, _) => (value, DateTimeOffset.Now));
        }

        /// <summary>
        /// Periodically cleans up expired cache entries.
        /// </summary>
        /// <param name="state">An object containing application-specific information relevant to the method invoked by this delegate, or null.</param>
        private void CleanupCache(object state)
        {
            var expiredKeys = _cache.Where(kv => DateTimeOffset.Now - kv.Value.LastUse > _expirationTimespan)
                                   .Select(kv => kv.Key)
                                   .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out var _);
            }
        }

        /// <summary>
        /// Clears all the key-value pairs in the cache.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Releases the resources used by the <see cref="LazyCacheDictionary{TKey, TValue}"/>.
        /// </summary>
        public void Dispose()
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            _cleanupTimer?.Dispose();
        }
    }
}