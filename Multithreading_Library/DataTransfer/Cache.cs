using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// a simple, threadsafe cache implementation utilizing MemoryCache
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class Cache<TItem, TKey> where TKey : notnull
    {
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new ConcurrentDictionary<TKey, SemaphoreSlim>();

        public async Task SetValue(TKey key, TItem value)
        {
            SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            await mylock.WaitAsync();
            try
            {
                _cache.Set(key, value);
            }
            finally
            {
                mylock.Release();
            }
        }
        public TItem? GetValue(TKey key)
        {
            if (_cache.TryGetValue(key, out TItem? cacheEntry))// Look for cache key.
            {
                return cacheEntry;
            }
            return default(TItem);
        }

        public bool GetValue(ref TKey key, ref TItem? outValue)
        {

            if (_cache.TryGetValue(key, out outValue))// Look for cache key.
            {
                return true;
            }
            return false;
        }
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
        public TKey[] GetAllKeys()
        {
            return _locks.Keys.ToArray();
        }
    }
}
