# Multithreading_Library

This library provides utilities to handle various multithreading scenarios in .NET applications. It contains the following classes:

1. **OneWriteMultiRead**: This class resolves performance limitations when transferring data between threads. 
The main challenge with multiple readers is that they can block the writer from updating a variable, leading to contention. `OneWriteMultiRead` allows reader threads to access the shared data with minimal performance impact while enabling an arbitrary number of threads to read the current value simultaneously.

```csharp
// Example usage
OneWrite_MultiRead<decimal> sharedDecimal = new OneWrite_MultiRead<decimal>(100);

/// Reader threads
decimal t = sharedDecimal.Value;

// Writer thread
sharedDecimal.Value = VALUE1;
```

Please note that for mutable and reference types it is recommended to enable DeepClone (Default) this makes objects sort of threadsafe by copying them.  
Please note that each read value is then no longer Synchronized in between the threads. For immutable types such as simple data types (int, bool, ...) DeepCloning can be disabled.

Custom types and structs should implement the `IDeepCloneable<T>` interface when deepcopy is enabled for improved performance. Here's an example:
```
[Serializable]
internal class CloneableObject : IDeepCloneable<CloneableObject>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> Clothes { get; set; } = new List<string>();

    public CloneableObject DeepClone()
    {
        // Create a deep copy of the object
        var clone = new CloneableObject
        {
            Id = this.Id,
            Name = new string(this.Name.ToCharArray()) // Deep copy the string
        };
        foreach (string cloth in Clothes)
        {
            clone.Clothes.Add(new string(cloth.ToCharArray()));
        }
        return clone;
    }
}
[Serializable]
internal struct CloneableStruct : IDeepCloneable<CloneableStruct>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> Clothes { get; set; }
    public CloneableStruct DeepClone()
    {
        // Create a deep copy of the object
        var clone = new CloneableStruct
        {
            Id = this.Id,
            Name = new string(this.Name.ToCharArray()),
            Clothes = new List<string>()
        };
        foreach (string cloth in Clothes)
        {
            clone.Clothes.Add(new string(cloth.ToCharArray()));
        }
        return clone;
    }
}
```

You can also access the Cloning Extension directly as suggested in the xunit test:
```
[Fact]
public void DeepClone_CloneableType()
{
    // Arrange
    var original = new CloneableObject()
    {
        Id = 1,
        Name = "Original",
        Clothes = new() { "Pant", "Socks" }
    };

    // Act
    var cloned = Cloning.DeepClone(original);

    // Assert
    Assert.NotNull(cloned);
    Assert.NotSame(original, cloned); // Ensure it's a different instance
    Assert.Equal(original.Id, cloned.Id);
    Assert.Equal(original.Name, cloned.Name);


    // change values to make sure we are not destroying things
    cloned.Name = "Clone";
    cloned.Id = 2;
    cloned.Clothes.AddRange(new[] { "Shirt", "Glasses" });

    // Ensure that the string is deeply copied
    Assert.NotSame(original.Name, cloned.Name);
    Assert.NotEqual(original.Id, cloned.Id);
    Assert.NotEqual(original.Name, cloned.Name);
    Assert.NotEqual(original.Clothes.Count, cloned.Clothes.Count);
}
```
The Cloning Method is Based on Baksteen.Extensions.DeepCopy Published under MIT-License

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