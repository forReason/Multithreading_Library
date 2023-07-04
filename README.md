# Multithreading_Library
this is a very brief library for multithreading purposes.  
It currently contains two classes:  
- OneWriteMultiRead  
(have an object which can be read from multiple threads at the same time but only updated from one thread at a time)
- ID Locks  
(have locks accessible through a dictionary in order to loc specific tasks, names and so on)
- Async Helper
(allows to run an async Task and wait for the result in a synchronous method.( for example in a constructos, but should be avoided if possible))
```
int parameter = 5;
int result = AsyncHelper.RunSync(async () => await SomeAsyncMethod(parameter));
```
