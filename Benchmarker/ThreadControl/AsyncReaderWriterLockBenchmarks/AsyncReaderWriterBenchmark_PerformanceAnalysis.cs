using Multithreading_Library.ThreadControl;
public static class AsyncReaderWriterBenchmark_PerformanceAnalysis
{
    private static AsyncReaderWriterLock _asyncLock;
    private static int _resource = 0;
    
    public static int NumOperations = 100000;
    public static int NumTasks = 10;
    
    
    public static async Task RunMixedTestAsync()
    {
        var tasks = new List<Task>(NumTasks * 2); // Assuming 1000 writes and 1000 reads

        for (int i = 0; i < NumTasks; i++)
        {
            // Writers
            tasks.Add(WriteAsync() /* 0.3% total time */);

            // Readers
            tasks.Add(ReadAsync()/* 0.2% total time */);
        }

        await Task.WhenAll(tasks);
    }

    private static async Task WriteAsync()
    {
        IAsyncDisposable write ;
        for (int i = 0; i < NumOperations; i++)
        {

            write = await _asyncLock.EnterWriteAsync();
            try
            {
                _resource++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (write is null)
                {
                    {}
                }
                await write.DisposeAsync();
            }
        }
    }

    private static async Task ReadAsync()
    {
        IAsyncDisposable read;
        int res;
        for (int i = 0; i < NumOperations; i++)
        {
            read = await _asyncLock.EnterReadAsync();
            try
            {
                 res = _resource; // This return value could be used if needed
            }
            finally
            {
                await read.DisposeAsync();
            }
        }
    }

}
