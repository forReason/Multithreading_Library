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
3. `AsyncHelper`: This class provides utilities to run an asynchronous Task and wait for the result in a synchronous method. It should be used with caution as it can lead to potential deadlocks. Whenever possible, prefer keeping async code all the way up the call stack.
```
// Example usage
int parameter = 5;
int result = AsyncHelper.RunSync(async () => await SomeAsyncMethod(parameter));
```
