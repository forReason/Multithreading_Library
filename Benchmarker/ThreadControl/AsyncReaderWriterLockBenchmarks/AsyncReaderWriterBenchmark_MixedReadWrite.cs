using BenchmarkDotNet.Attributes;
using Multithreading_Library.ThreadControl;
using System.Threading.Tasks;
using System.Threading;

namespace Benchmarker.ThreadControl.AsyncReaderWriterLockBenchmarks;

[MemoryDiagnoser]
public class AsyncReaderWriterBenchmark_MixedReadWrite
{
    private AsyncReaderWriterLock _asyncLock;
    private ReaderWriterLockSlim _classicLock;
    private Microsoft.VisualStudio.Threading.AsyncReaderWriterLock _msAsyncLock;
    private int _resource;

    [Params(100, 1000)]
    public int NumOperations;

    [Params(10, 100)]
    public int WorkloadSize;

    [GlobalSetup]
    public void Setup()
    {
        _asyncLock = new AsyncReaderWriterLock();
        _classicLock = new ReaderWriterLockSlim();
        _msAsyncLock = new Microsoft.VisualStudio.Threading.AsyncReaderWriterLock();
        _resource = 0;
    }
    
    [Benchmark]
    public async Task MixedAsyncReaderWriterLockTest()
    {
        await RunMixedTestAsync(_asyncLock);
    }

    [Benchmark]
    public async Task MixedReaderWriterLockSlimTest()
    {
        // Execute synchronous lock operations in a background thread for fairness in comparison
        await Task.Run(() => RunMixedTestSync(_classicLock));
    }

    [Benchmark]
    public async Task MixedMsAsyncReaderWriterLockTest()
    {
        await RunMixedTestAsync(_msAsyncLock);
    }

    private async Task RunMixedTestAsync(AsyncReaderWriterLock lockObj)
    {
        var tasks = new Task[NumOperations * 2]; // For a mix of readers and writers

        for (int i = 0; i < NumOperations; i++)
        {
            int index = i;
            // Writers
            tasks[index * 2] = Task.Run(async () =>
            {
                await using (await lockObj.EnterWriteAsync())
                {
                    Thread.Sleep(WorkloadSize); // Simulate work
                    _resource++;
                }
            });

            // Readers
            tasks[index * 2 + 1] = Task.Run(async () =>
            {
                await using (await lockObj.EnterReadAsync())
                {
                    await Task.Delay(WorkloadSize); // Simulate reading workload
                }
            });
        }

        await Task.WhenAll(tasks);
    }

    private void RunMixedTestSync(ReaderWriterLockSlim lockObj)
    {
        var tasks = new Task[NumOperations * 2];

        for (int i = 0; i < NumOperations; i++)
        {
            int index = i;
            // Writers
            tasks[index * 2] = Task.Run(() =>
            {
                lockObj.EnterWriteLock();
                try
                {
                    Thread.Sleep(WorkloadSize);
                    _resource++;
                }
                finally { lockObj.ExitWriteLock(); }
            });

            // Readers
            tasks[index * 2 + 1] = Task.Run(() =>
            {
                lockObj.EnterReadLock();
                try { Thread.Sleep(WorkloadSize); }
                finally { lockObj.ExitReadLock(); }
            });
        }

        Task.WaitAll(tasks);
    }

    private async Task RunMixedTestAsync(Microsoft.VisualStudio.Threading.AsyncReaderWriterLock msLockObj)
    {
        var tasks = new Task[NumOperations * 2]; // Adjust for mixed read and write

        for (int i = 0; i < NumOperations; i++)
        {
            int index = i;
            // Writers
            tasks[index * 2] = Task.Run(async () =>
            {
                using (await msLockObj.WriteLockAsync())
                {
                    Thread.Sleep(WorkloadSize); // Simulate work
                    _resource++;
                }
            });

            // Readers
            tasks[index * 2 + 1] = Task.Run(async () =>
            {
                using (await msLockObj.ReadLockAsync())
                {
                    await Task.Delay(WorkloadSize); // Simulate reading workload
                }
            });
        }

        await Task.WhenAll(tasks);
    }
}
