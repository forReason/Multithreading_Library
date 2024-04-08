using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Xunit;
using Multithreading_Library.ThreadControl;
namespace Multithreading_Unit_Tests.ThreadControl;

public class AsyncReaderWriterLockTests
{
    [Fact]
    public async Task MultipleReaders_CanReadConcurrently()
    {
        var lockObj = new AsyncReaderWriterLock();
        var readTasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                await using (await lockObj.EnterReadAsync())
                {
                    Assert.True(lockObj.ReadersActive > 0);
                    Assert.Equal(0, lockObj.WritersActive);
                }
            }));
        }

        await Task.WhenAll(readTasks);

        Assert.Equal(0, lockObj.ReadersActive);
        Assert.Equal(0, lockObj.WritersActive);
    }

    [Fact]
    public async Task SingleWriter_HasExclusiveAccess()
    {
        var lockObj = new AsyncReaderWriterLock();
        
        var writer = Task.Run(async () =>
        {
            await using (await lockObj.EnterWriteAsync())
            {
                Assert.Equal(1, lockObj.WritersActive);
                Assert.Equal(0, lockObj.ReadersActive);
                await Task.Delay(1000);
            }
        });

        await Task.Delay(50);
        var reader = Task.Run(async () =>
        {
            await using (await lockObj.EnterReadAsync())
            {
                Assert.Equal(1, lockObj.WritersActive);
                Assert.Equal(0, lockObj.ReadersActive);
                await Task.Delay(1000);
            }
        });
        await Task.Delay(50);
        
        Assert.Equal(1, lockObj.WritersActive);
        Assert.Equal(0, lockObj.ReadersActive);
    }

    [Fact]
    public async Task WriterWaits_ForReadersToComplete()
    {
        var lockObj = new AsyncReaderWriterLock();
        var completedReads = 0;

        // Start reading
        var reader = Task.Run(async () =>
        {
            await using (await lockObj.EnterReadAsync())
            {
                await Task.Delay(1000); // Simulate reading work
                completedReads++;
            }
        });

        // Give the reader a moment to start
        await Task.Delay(50);

        // Attempt to write
        var writer = Task.Run(async () =>
        {
            await using (await lockObj.EnterWriteAsync())
            {
                Assert.Equal(1, lockObj.WritersActive);
                Assert.Equal(0, lockObj.ReadersActive);
            }
        });
        await Task.Delay(50);

        await Task.WhenAll(reader, writer);

        Assert.Equal(1, completedReads);
        Assert.Equal(0, lockObj.ReadersActive);
        Assert.Equal(0, lockObj.WritersActive);
    }

    [Fact]
    public async Task ReadersAndWriters_SwitchBackAndForth()
    {
        var lockObj = new AsyncReaderWriterLock();
        int actionsCompleted = 0;
        int readActionsCompleted = 0; 
        int writeActionsCompleted = 0; 

        // Start a writer first
        var writeFirst = Task.Run(async () =>
        {
            await using (await lockObj.EnterWriteAsync())
            {
                Assert.Equal(1, lockObj.WritersActive);
                Assert.Equal(AsyncReaderWriterLock.ReaderWriterState.Writing, lockObj.CurrentState);
                Assert.Equal(0, lockObj.ReadersActive);
                Interlocked.Increment(ref actionsCompleted);
                Interlocked.Increment(ref writeActionsCompleted);
            }
        });

        // Wait for the writer to queue
        await Task.Delay(50);

        // Then start readers
        var readTasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            readTasks.Add(Task.Run(async () =>
            {
                await using (await lockObj.EnterReadAsync())
                {
                    Assert.True(lockObj.ReadersActive > 0);
                    Assert.Equal(AsyncReaderWriterLock.ReaderWriterState.Reading, lockObj.CurrentState);
                    Assert.Equal(0, lockObj.WritersActive);
                    Interlocked.Increment(ref actionsCompleted);
                    Interlocked.Increment(ref readActionsCompleted);
                }
            }));
        }

        // Wait a bit and start another writer
        await Task.Delay(100);
        
        var writeSecond = Task.Run(async () =>
        {
            await using (await lockObj.EnterWriteAsync())
            {
                Assert.Equal(1, lockObj.WritersActive);
                Assert.Equal(AsyncReaderWriterLock.ReaderWriterState.Writing, lockObj.CurrentState);
                Assert.Equal(0, lockObj.ReadersActive);
                Interlocked.Increment(ref actionsCompleted);
                Interlocked.Increment(ref writeActionsCompleted);
            }
        });
        
        await Task.WhenAll(readTasks);
        Assert.Equal(0, lockObj.WritersActive);
        Assert.Equal(AsyncReaderWriterLock.ReaderWriterState.Inactive, lockObj.CurrentState);
        Assert.Equal(0, lockObj.ReadersActive);
        await writeFirst;
        await writeSecond;

        Assert.Equal(7, actionsCompleted); // 2 writers and 5 readers
        Assert.Equal(2, writeActionsCompleted); // 2 writers and 5 readers
        Assert.Equal(5, readActionsCompleted); // 2 writers and 5 readers
    }
    [Fact]
    public async Task HighVolume_ReadersWithIntermittentWriters()
    {
        var lockObj = new AsyncReaderWriterLock();
        int readersCompleted = 0;
        int writersCompleted = 0;
        int numberOfReaders = 100;
        int numberOfWriters = 10;
        var tasks = new List<Task>();

        // Launch a high volume of readers
        for (int i = 0; i < numberOfReaders; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using (await lockObj.EnterReadAsync())
                {
                    // Simulate work by delaying
                    await Task.Delay(5);
                    Interlocked.Increment(ref readersCompleted);
                }
            }));
        }

        // Intermittently launch writers
        for (int i = 0; i < numberOfWriters; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using (await lockObj.EnterWriteAsync())
                {
                    // Simulate work by delaying
                    await Task.Delay(20);
                    Interlocked.Increment(ref writersCompleted);
                }
            }));

            // Space out writer starts slightly
            if (i % 2 == 0) await Task.Delay(50);
        }

        // Wait for all operations to complete
        await Task.WhenAll(tasks);

        Assert.Equal(numberOfReaders, readersCompleted);
        Assert.Equal(numberOfWriters, writersCompleted);
    }
    [Fact]
    public async Task HeavyConcurrent_ReadersAndWriters()
    {
        var lockObj = new AsyncReaderWriterLock();
        int readersCompleted = 0;
        int writersCompleted = 0;
        int totalOperations = 100;
        var tasks = new List<Task>();

        for (int i = 0; i < totalOperations; i++)
        {
            // Alternate between readers and writers for each operation
            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await using (await lockObj.EnterReadAsync())
                    {
                        // Simulate work by delaying
                        await Task.Delay(1);
                        Interlocked.Increment(ref readersCompleted);
                    }
                }));
            }
            else
            {
                tasks.Add(Task.Run(async () =>
                {
                    await using (await lockObj.EnterWriteAsync())
                    {
                        // Simulate work by delaying
                        await Task.Delay(1);
                        Interlocked.Increment(ref writersCompleted);
                    }
                }));
            }
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Since operations were launched in a balanced manner,
        // the number of completed readers and writers should be equal.
        Assert.True(readersCompleted > 0 && writersCompleted > 0);
        Assert.Equal(totalOperations / 2, readersCompleted);
        Assert.Equal(totalOperations / 2, writersCompleted);
    }
    [Fact]
    public async Task StressTest_ForInconsistenciesWithHighConcurrency()
    {
        var lockObj = new AsyncReaderWriterLock();
        var inconsistencyDetected = false;
        int totalReaders = 500;
        int totalWriters = 100;
        var readerTasks = new List<Task>();
        var writerTasks = new List<Task>();

        // Define a shared resource for the test
        int sharedResource = 0;

        // Reader work simulating reading the shared resource
        Func<Task> readerWork = async () =>
        {
            await using (await lockObj.EnterReadAsync())
            {
                // Simulate the read by checking the shared resource value
                if (sharedResource != 0)
                {
                    inconsistencyDetected = true;
                }
            }
        };

        // Writer work simulating an update to the shared resource
        Func<Task> writerWork = async () =>
        {
            await using (await lockObj.EnterWriteAsync())
            {
                // Increment the resource, wait, then decrement, simulating a write operation
                Interlocked.Increment(ref sharedResource);
                // Ensure there's enough time for potential concurrent access to cause issues
                await Task.Delay(10); 
                Interlocked.Decrement(ref sharedResource);
            }
        };

        // Launch reader tasks
        for (int i = 0; i < totalReaders; i++)
        {
            readerTasks.Add(Task.Run(readerWork));
        }

        // Launch writer tasks
        for (int j = 0; j < totalWriters; j++)
        {
            writerTasks.Add(Task.Run(writerWork));
        }

        // Wait for all reader and writer tasks to complete
        await Task.WhenAll(readerTasks.Concat(writerTasks));

        // Check for inconsistency
        Assert.False(inconsistencyDetected, "Inconsistency detected: A reader read a value that should only be possible during a write operation.");
    }
    [Fact]
    public async Task TestForTornValues()
    {
        int iterations = 10000;
        var resource = new long();
        bool tornReadDetected = false;
        var lockObj = new AsyncReaderWriterLock();

        // Task to simulate non-atomic updates to the shared resource
        var writerTask = Task.Run(async () =>
        {
            for (long i = 0; i < iterations; i++)
            {
                long combinedValue = (i << 32) | i;
                await using (await lockObj.EnterWriteAsync())
                {
                    resource = combinedValue;
                }
            }
        });

        // Task to read and verify the shared resource's consistency
        var readerTask = Task.Run(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                long valueRead;
                await using (await lockObj.EnterWriteAsync())
                {
                    valueRead = resource;
                }

                long lowerPart = valueRead & 0xFFFFFFFF;
                long upperPart = valueRead >> 32;

                if (lowerPart != upperPart)
                {
                    tornReadDetected = true;
                    break;
                }
            }
        });

        await Task.WhenAll(writerTask, readerTask);

        Assert.False(tornReadDetected, "Detected a torn read, indicating a non-atomic update or read.");
    }
}