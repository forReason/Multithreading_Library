using BenchmarkDotNet.Attributes;
using Multithreading_Library.ThreadControl;

namespace Benchmarker.ThreadControl.AsyncReaderWriterLockBenchmarks;

[MemoryDiagnoser]
public class AsyncReaderWriterBenchmark_IoAndCpuWorkBenchmark
{
    private AsyncReaderWriterLock _asyncLock = new ();
    private ReaderWriterLockSlim _rwLockSlim = new ();
    private Microsoft.VisualStudio.Threading.AsyncReaderWriterLock _vsAsyncLock = new ();

    [Params(100000)] // Adjusted for clarity and testing different scales
    public int WorkItemCount;

    // Simulated I/O Duration in milliseconds
    private const int IoOperationDuration = 1000; 
    // Simulated reading operation duration in milliseconds
    private const int ReadOperationDuration = 100; 

    [Benchmark]
    public Task WithAsyncReaderWriterLock() => RunBenchmarkAsync(_asyncLock);

    [Benchmark]
    public Task WithReaderWriterLockSlim() => RunBenchmarkAsync(_rwLockSlim);

    [Benchmark]
    public Task WithVsThreadingAsyncReaderWriterLock() => RunBenchmarkAsync(_vsAsyncLock);

    private async Task RunBenchmarkAsync(object lockType)
    {
        Task writerTask, readerTask, unrelatedWorkTask;

        switch (lockType)
        {
            case AsyncReaderWriterLock asyncLock:
                writerTask = WriteWithAsyncLock(asyncLock);
                readerTask = ReadWithAsyncLock(asyncLock);
                break;
            case ReaderWriterLockSlim rwLockSlim:
                writerTask = Task.Run(() => WriteWithRwLockSlim(rwLockSlim));
                readerTask = Task.Run(() => ReadWithRwLockSlim(rwLockSlim));
                break;
            case Microsoft.VisualStudio.Threading.AsyncReaderWriterLock vsAsyncLock:
                writerTask = WriteWithVsAsyncLock(vsAsyncLock);
                readerTask = ReadWithVsAsyncLock(vsAsyncLock);
                break;
            default:
                throw new ArgumentException("Unsupported lock type.");
        }

        unrelatedWorkTask = PerformUnrelatedWorkAsync(WorkItemCount);

        await Task.WhenAll(writerTask, readerTask, unrelatedWorkTask);
    }

    private async Task WriteWithAsyncLock(AsyncReaderWriterLock lockObj)
    {
        await using (await lockObj.EnterWriteAsync())
        {
            await Task.Delay(IoOperationDuration);
        }
    }

    private void WriteWithRwLockSlim(ReaderWriterLockSlim lockObj)
    {
        lockObj.EnterWriteLock();
        try { Task.Delay(IoOperationDuration).Wait(); }
        finally { lockObj.ExitWriteLock(); }
    }

    private async Task WriteWithVsAsyncLock(Microsoft.VisualStudio.Threading.AsyncReaderWriterLock lockObj)
    {
        using (await lockObj.WriteLockAsync())
        {
            await Task.Delay(IoOperationDuration);
        }
    }
    private async Task ReadWithAsyncLock(AsyncReaderWriterLock lockObj)
    {
        await using (await lockObj.EnterReadAsync())
        {
            await Task.Delay(ReadOperationDuration); // Simulate reading operation
        }
    }

    private void ReadWithRwLockSlim(ReaderWriterLockSlim lockObj)
    {
        lockObj.EnterReadLock();
        try { Task.Delay(ReadOperationDuration).Wait(); } // Simulate reading operation
        finally { lockObj.ExitReadLock(); }
    }

    private async Task ReadWithVsAsyncLock(Microsoft.VisualStudio.Threading.AsyncReaderWriterLock lockObj)
    {
        using (await lockObj.ReadLockAsync())
        {
            await Task.Delay(ReadOperationDuration); // Simulate reading operation
        }
    }
    

    private async Task PerformUnrelatedWorkAsync(int workCount)
    {
        for (int i = 0; i < workCount; i++)
        {
            await Task.Yield();
        }
    }
}