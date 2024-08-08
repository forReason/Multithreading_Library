using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// a simple, thread safe cache implementation utilizing MemoryCache like a dictionary
    /// </summary>
    /// <remarks>basically the Set operators are overridden since Memory Cache does not prevent race conditions on Set Operations</remarks>
    /// <typeparam name="TItem">the Value Type</typeparam>
    /// <typeparam name="TKey">the Key Type</typeparam>
    public class Cache<TItem, TKey> where TKey : notnull
    {
        private readonly MemoryCache _cache = new (new MemoryCacheOptions());
        private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new();

        /// <summary>
        /// sets the cache Value in a thread safe manner
        /// </summary>
        /// <param name="key">the unique identifier of the cache item</param>
        /// <param name="value">the value to set</param>
        public async Task SetValue(TKey key, TItem value)
        {
            SemaphoreSlim itemLock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            await itemLock.WaitAsync();
            try
            {
                _cache.Set(key, value);
            }
            finally
            {
                itemLock.Release();
            }
        }
        
        /// <summary>
        /// attempts to retrieve a value
        /// </summary>
        /// <remarks>for Nullable Types, it is strongly recommended to use TryGet</remarks>
        /// <param name="key"></param>
        /// <returns>returns null if the value was null or couldn't be obtained</returns>
        public TItem? GetValue(TKey key)
        {
            if (_cache.TryGetValue(key, out TItem? cacheEntry))// Look for cache key.
            {
                return cacheEntry;
            }
            return default(TItem);
        }

        /// <summary>
        /// Attempts to retrieve a value from a key
        /// </summary>
        /// <param name="key">the key to explore</param>
        /// <param name="outValue">the value to be written to</param>
        /// <returns>a boolean indicating if the action was successful or not</returns>
        public bool TryGetValue(ref TKey key, ref TItem? outValue)
        {
            if (_cache.TryGetValue(key, out outValue))// Look for cache key.
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// returns all values of the cache
        /// </summary>
        /// <returns></returns>
        public TItem?[] GetAllValues()
        {
            TKey[] keys = GetAllKeys();
            TItem?[] values = new TItem[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                _cache.TryGetValue(keys[i], out values[i]);
            }
            return values;
        }
        /// <summary>
        /// returns all keys from the cache
        /// </summary>
        /// <returns></returns>
        public TKey[] GetAllKeys()
        {
            return _locks.Keys.ToArray();
        }
    }
}
