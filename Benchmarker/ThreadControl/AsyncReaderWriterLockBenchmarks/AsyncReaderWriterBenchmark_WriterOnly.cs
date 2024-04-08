using BenchmarkDotNet.Attributes;
using AsyncReaderWriterLock = Multithreading_Library.ThreadControl.AsyncReaderWriterLock;

namespace Benchmarker.ThreadControl.AsyncReaderWriterLockBenchmarks;

[MemoryDiagnoser]
public class AsyncReaderWriterBenchmark_WriterOnly
{
    private AsyncReaderWriterLock _asyncLock;
    private ReaderWriterLockSlim _classicLock;
    private Microsoft.VisualStudio.Threading.AsyncReaderWriterLock _msAsyncLock; // Microsoft's AsyncReaderWriterLock
    private int _resource;

    [Params(100, 10000)]
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
    public async Task AsyncReaderWriterLockTest()
    {
        await RunTestAsync(_asyncLock);
    }

    [Benchmark]
    public async Task ReaderWriterLockSlimTest()
    {
        // Running as async to standardize the benchmark interface, but operations are synchronous
        await Task.Run(() => RunTestSync(_classicLock));
    }

    [Benchmark]
    public async Task MsAsyncReaderWriterLockTest()
    {
        await RunTestAsync(_msAsyncLock);
    }

    private async Task RunTestAsync(AsyncReaderWriterLock asyncLock)
    {
        var tasks = new Task[NumOperations];
        for (int i = 0; i < NumOperations; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await using (await asyncLock.EnterWriteAsync())
                {
                    for (int w = 0; w < WorkloadSize; w++)
                    {
                        await Task.Delay(WorkloadSize);
                        _resource++;
                    }
                }
            });
        }

        await Task.WhenAll(tasks);
    }

    private void RunTestSync(ReaderWriterLockSlim classicLock)
    {
        var tasks = new Task[NumOperations];
        for (int i = 0; i < NumOperations; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                classicLock.EnterWriteLock();
                try
                {
                    for (int w = 0; w < WorkloadSize; w++)
                    {
                        Task.Delay(WorkloadSize).Wait();
                        _resource++;
                    }
                }
                finally
                {
                    classicLock.ExitWriteLock();
                }
            });
        }

        Task.WaitAll(tasks);
    }

    private async Task RunTestAsync(Microsoft.VisualStudio.Threading.AsyncReaderWriterLock msAsyncLock)
    {
        var tasks = new Task[NumOperations];
        for (int i = 0; i < NumOperations; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using (await msAsyncLock.WriteLockAsync())
                {
                    for (int w = 0; w < WorkloadSize; w++)
                    {
                        await Task.Delay(WorkloadSize);
                        _resource++;
                    }
                }
            });
        }

        await Task.WhenAll(tasks);
    }
}