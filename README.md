# Multithreading_Library

This library provides utilities to handle various multithreading scenarios in .NET applications. It contains the following classes:

1. `OneWriteMultiRead`: This class allows an object to be read from multiple threads at the same time but only updated from one thread at a time. This helps to ensure data integrity when working with shared resources across multiple threads.

```csharp
// Example usage
OneWrite_MultiRead<decimal> sharedDecimal = new OneWrite_MultiRead<decimal>(100);

/// Reader threads
decimal t = sharedDecimal.Value;

// writer thread
sharedDecimal.Value = VALUE1;
```

2. `IDLocks`: This class provides locks that are accessible through a dictionary. This allows specific tasks, names, or other entities to be locked individually, enabling finer control over thread synchronization.
```
// Example usage
IDLocks<int> idLocks = new IDLocks<int>();
var lockObject = idLocks.ObtainLockObject(5);
```

3. `RequestIDGenerator` this class returns an incremental, threadsafe id which can be used to identify requests. EG for a websocket. The function rolls over to 0 at int.MaxValue
```
// Example usage
RequestIDGenerator idGenerator = new RequestIDGenerator();
int id = idGenerator.GetNextRequestId()
```

4. `AsyncHelper`: This class provides utilities to run an asynchronous Task and wait for the result in a synchronous method. It should be used with caution as it can lead to potential deadlocks. Whenever possible, prefer keeping async code all the way up the call stack.
```
// Example usage
int parameter = 5;
int result = AsyncHelper.RunSync(async () => await SomeAsyncMethod(parameter));
```

5. `Caching`: The library includes examples of lightweight caching mechanisms.

    - `LazyCache\<T\>`: A simple cache that holds a value for a specified timespan. After the timespan expires, it returns null or default.

        ```csharp
        // Example usage
        LazyCache<int?> numberCache = new (TimeSpan.FromMinutes(5)); // values are valid for 5 Minutes
        numberCache.Value = 42; // Setting a value
        int? cachedNumber = numberCache.Value; // Retrieving the value
        if (cachedNumber == null) // set new value if cache has expired
            cachedNumber = FetchReason("Life");
        ```

    - `LazyCacheDictionary\<TKey, TValue\>`: A thread-safe, lazy-loading cache dictionary with expiration for each key. It supports automatic cleanup of expired entries.

        ```csharp
        // Example usage
    
        // individual item validity time = 5 Minutes
        // cache cleanup interval = 1 Hour
        LazyCacheDictionary<string, int?> dictionaryCache = new (TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
    
        dictionaryCache.Set("key1", 100); // Adding a value
        int? value = dictionaryCache.Get("key1"); // Retrieving a value
        if (value == null) // set new value if cache has expired
            dictionaryCache.Set("Life", 42);
        ```