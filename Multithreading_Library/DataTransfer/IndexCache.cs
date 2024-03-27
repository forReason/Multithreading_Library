using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// Provides a thread-safe index cache mechanism for storing and retrieving items.<br/>
    /// Each Item retries a unique index value, which can be looked up quickly in both directions
    /// </summary>
    /// <typeparam name="TItem">The type of the items to be cached. Must not be null.</typeparam>
    public class IndexCache<TItem> where TItem : notnull
    {
        private readonly MemoryCache _valueCache = new MemoryCache(new MemoryCacheOptions());
        private readonly MemoryCache _indexCache = new MemoryCache(new MemoryCacheOptions());
        /// <summary>
        /// A thread-safe dictionary to hold the locks for each key. This ensures that only one operation can modify the cache at a time for a given key.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        /// <summary>
        /// A counter to keep track of the next available index. It is protected for thread safety.
        /// </summary>
        public int Count => _count;
        private int _count;
        /// <summary>
        /// Retrieves the index associated with a specific item from the cache.
        /// If the item is not found in the cache, it creates a new index, adds the item to the cache, and returns the new index.
        /// This operation is thread-safe and ensures that each item is associated with a unique index.
        /// </summary>
        /// <param name="createItem">The item to retrieve or add to the cache.</param>
        /// <returns>The index of the item in the cache.</returns>
        /// <exception cref="NullReferenceException">Thrown when <paramref name="createItem"/> is null.</exception>
        public async Task<int> GetOrCreateIndex(TItem createItem)
        {
            if (createItem == null) throw new NullReferenceException("createItem may not be null!");

            if (_indexCache.TryGetValue(createItem.ToString()!, out int cacheEntry)) return cacheEntry; // Look for cache key.
            SemaphoreSlim indexLock = _locks.GetOrAdd(createItem.ToString()!, _ => new SemaphoreSlim(1, 1));
            await indexLock.WaitAsync();
            try
            {
                if (!_indexCache.TryGetValue(createItem.ToString()!, out cacheEntry))
                {
                    cacheEntry = Interlocked.Increment(ref _count) -1;
                    _indexCache.Set(createItem.ToString()!, cacheEntry);
                    _valueCache.Set(cacheEntry, createItem);
                }
            }
            finally
            {
                indexLock.Release();
            }
            return cacheEntry;
        }
        /// <summary>
        /// Retrieves an item by its index from the cache.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>The item associated with the specified index, if found; otherwise, null.</returns>
        public TItem? GetItemByIndex(int index)
        {
            _valueCache.TryGetValue(index, out TItem? result);
            return result;
        }
    }
}
