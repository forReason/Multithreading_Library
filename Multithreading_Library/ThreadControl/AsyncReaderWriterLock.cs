namespace Multithreading_Library.ThreadControl;

/// <summary>
/// <see cref="AsyncReaderWriterLock"/> is made for high concurrency scenarios with potential multiple readers and writers<br/>
/// The Class allows concurrent readers but only one writer at a time.<br/>
/// In very high concurrency scenarios with both, readers and writers,
/// the class switches back and forth between batched reading and writing to ensure fairness
/// </summary>
public class AsyncReaderWriterLock
{
    private readonly SemaphoreSlim _ReaderQueue = new (0,int.MaxValue);
    private readonly SemaphoreSlim _WriterQueue = new (0,1);

    /// <summary>
    /// a snapshot of the current active reader count
    /// </summary>
    public int ReadersActive => _ReadersActive;
    private int _ReadersActive = 0;

    /// <summary>
    /// a Snapshot of the current active writer count (max 1)
    /// </summary>
    public int WritersActive => _WritersActive;
    private int _WritersActive = 0;

    /// <summary>
    /// a snapshot of the current number of writers which are waiting to enter write
    /// </summary>
    public int WritersWaiting => _WritersWaiting;
    private int _WritersWaiting = 0;

    /// <summary>
    /// a snapshot of the current number of readers which are waiting to enter read
    /// </summary>
    public int ReadersWaiting => _ReadersWaiting;
    private int _ReadersWaiting = 0;
    
    /// <summary>
    /// 0: inactive<br/>
    /// 1: reading<br/>
    /// 2: writing
    /// </summary>
    private int _State = 0;

    /// <summary>
    /// a snapshot of the current state of processing
    /// </summary>
    public ReaderWriterState CurrentState => (ReaderWriterState)_State;

    /// <summary>
    /// releases the next batch of waiting readers
    /// </summary>
    private void ReleaseReaders()
    {
        Interlocked.Exchange(ref _State, (int)ReaderWriterState.Reading);
        _ReaderQueue.Release(Interlocked.Exchange(ref _ReadersWaiting, 0));
    }

    /// <summary>
    /// releases the next waiting writer
    /// </summary>
    private void ReleaseWriter()
    {
        Interlocked.Exchange(ref _State, (int)ReaderWriterState.Writing);
        _WriterQueue.Release();
    }
    
    /// <summary>
    /// waits until <see cref="ReleaseReaders"/> is executed
    /// </summary>
    private async Task WaitForReaderSlot()
    {
        Interlocked.Increment(ref _ReadersWaiting);
        await _ReaderQueue.WaitAsync();
    }
    /// <summary>
    /// waits until the own thread receives a slot by calling <see cref="ReleaseWriter"/>
    /// </summary>
    private async Task WaitForWriterSlot()
    {
        Interlocked.Increment(ref _WritersWaiting);
        await _WriterQueue.WaitAsync();
        Interlocked.Decrement(ref _WritersWaiting);
    }

    /// <summary>
    /// Enters the reader queue and returns when reading is allowed. 
    /// This method allows concurrent reading operations but ensures exclusive writing access by waiting for any active writer to complete before entering the reading state.
    /// When invoked, it either immediately grants access to start reading if no writers are active or waits asynchronously until reading is safe.
    /// </summary>
    /// <returns>
    /// A task that completes with an <see cref="IAsyncDisposable"/>. Disposing the returned object marks the reading process as complete and potentially allows waiting writers to proceed.
    /// </returns>
    /// <example>
    /// Here's how you can use <see cref="EnterReadAsync"/> in an asynchronous method to safely read from a shared resource:
    /// <code>
    /// // Enter the reading state and get a releaser for when reading is complete
    /// await using (await EnterReadAsync())
    /// {
    ///     // Perform the reading operation here
    ///     Console.WriteLine("Reading shared data...");
    ///     // The shared resource can be safely read within this block
    /// }
    /// // Upon disposing of the releaser, the reading state is marked as complete
    /// </code>
    /// This method ensures that multiple read operations can occur concurrently without interfering with exclusive write operations, maintaining data integrity.
    /// </example>
    public async Task<IAsyncDisposable> EnterReadAsync()
    {
        
        while (true)
        {
            ReaderWriterState initialReaderWriterState = (ReaderWriterState)Interlocked.CompareExchange(
                    ref _State, 
                    (int)ReaderWriterState.Reading, 
                    (int)ReaderWriterState.Inactive);
            if (initialReaderWriterState == ReaderWriterState.Writing)
                // wait until writer is complete
            {
                await WaitForReaderSlot();
                Interlocked.Increment(ref _ReadersActive);
                break;
            }
            else if (initialReaderWriterState == ReaderWriterState.Reading && _WritersWaiting == 0)
                // start safely when no writer is waiting
            {
                if (Interlocked.Increment(ref _ReadersActive) > 0)
                    // it is safe to start reading, since there are still active readers
                {
                    break;
                }

                // it is unsafe to start reading. might collide with Releaser.
                // rollback and wait for next Writer cycle
                Interlocked.Decrement(ref _ReadersActive);
                await WaitForReaderSlot();
                Interlocked.Increment(ref _ReadersActive);
                break;
            }
            else // initialReaderWriterState == ReaderWriterState.Inactive
            {
                Interlocked.Increment(ref _ReadersActive);
                break;
            }
        }
        
        return new Releaser(async () =>
        {
            if (Interlocked.Decrement(ref _ReadersActive) == 0)
            {
                if (_WritersWaiting > 0) ReleaseWriter();
                else if (_ReadersWaiting > 0) ReleaseReaders();
                else Interlocked.Exchange(ref _State, (int)ReaderWriterState.Inactive);
                await Task.CompletedTask;
            }
        });
    }

    /// <summary>
    /// Enters the write queue and returns when writing is allowed.
    /// This method ensures exclusive writing access by waiting for any active readers or writers to complete before entering the writing state.
    /// When invoked, it waits asynchronously until it's safe to start writing, ensuring exclusive access to the shared resource.
    /// </summary>
    /// <returns>
    /// A task that completes with an <see cref="IAsyncDisposable"/>. Disposing the returned object marks the writing process as complete and potentially allows waiting readers or writers to proceed.
    /// </returns>
    /// <example>
    /// Here's how you can use <see cref="EnterWriteAsync"/> in an asynchronous method to safely write to a shared resource:
    /// <code>
    /// // Enter the writing state and get a releaser for when writing is complete
    /// await using (await EnterWriteAsync())
    /// {
    ///     // Perform the writing operation here
    ///     Console.WriteLine("Writing to shared data...");
    ///     // The shared resource can be safely written within this block
    /// }
    /// // Upon disposing of the releaser, the writing state is marked as complete
    /// </code>
    /// This method ensures that the write operation occurs exclusively, without interference from other read or write operations, maintaining data integrity.
    /// </example>
    public async Task<IAsyncDisposable> EnterWriteAsync()
    {
        
        while (true)
        {
            ReaderWriterState initialReaderWriterState =
                (ReaderWriterState)Interlocked.CompareExchange(
                    ref _State, 
                    (int)ReaderWriterState.Writing, 
                    (int)ReaderWriterState.Inactive);
            if (initialReaderWriterState == ReaderWriterState.Writing)
            {
                await WaitForWriterSlot();
                Interlocked.Exchange(ref _State, (int)ReaderWriterState.Writing);
                break;
            }
            else if (initialReaderWriterState == ReaderWriterState.Reading)
            {
                await WaitForWriterSlot();
                Interlocked.Exchange(ref _State, (int)ReaderWriterState.Writing);
                break;
            }
            else break;
        }

        Interlocked.Increment(ref _WritersActive);
        
        return new Releaser(async () =>
        {
            Interlocked.Decrement(ref _WritersActive);
            if (_ReadersWaiting > 0) ReleaseReaders();
            else if (_WritersWaiting > 0) ReleaseWriter();
            else Interlocked.Exchange(ref _State, (int)ReaderWriterState.Inactive);
            await Task.CompletedTask;
        });
    }
    
    /// <summary>
    /// disposable which marks the task as finished
    /// </summary>
    private class Releaser : IAsyncDisposable
    {
        private readonly Func<Task> _releaseAction;
        public Releaser(Func<Task> releaseAction)
        {
            _releaseAction = releaseAction;
        }

        public async ValueTask DisposeAsync()
        {
            if (_releaseAction != null)
            {
                await _releaseAction();
            }
        }
    }

    /// <summary>
    /// Defines the State for ReaderWriter Classes
    /// </summary>
    public enum ReaderWriterState
    {
        /// <summary>
        /// no current action
        /// </summary>
        Inactive = 0,
        /// <summary>
        /// currently reading
        /// </summary>
        Reading = 1,
        /// <summary>
        /// currently writing
        /// </summary>
        Writing = 2
    }
}