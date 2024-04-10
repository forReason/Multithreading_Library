namespace Multithreading_Library.ThreadControl;

/// <summary>
/// <see cref="AsyncReaderWriterLock"/> is made for high concurrency scenarios with potential multiple readers and writers<br/>
/// The Class allows concurrent readers but only one writer at a time.<br/>
/// In very high concurrency scenarios with both, readers and writers,
/// the class switches back and forth between batched reading and writing to ensure fairness
/// </summary>
public class AsyncReaderWriterLock
{
    private readonly AwaitableSignalSlim _ReaderQueue = new AwaitableSignalSlim();
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
    public int ReadersWaiting => _ReaderQueue.Waiting;
    
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
        _ReaderQueue.FireEvent();
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
    private async Task<bool> WaitForReaderSlot(TimeSpan? timeOut, CancellationToken? cancellation)
    {
        return await _ReaderQueue.AwaitSignalAsync(timeOut, cancellation);
    }
    /// <summary>
    /// waits until the own thread receives a slot by calling <see cref="ReleaseWriter"/>
    /// </summary>
    private async Task<bool> WaitForWriterSlot(TimeSpan? timeOut = null, CancellationToken? cancellation = null)
    {
        Interlocked.Increment(ref _WritersWaiting);
        bool success = false;
        try
        {
            if (timeOut is null)
            {
                await _WriterQueue.WaitAsync(cancellation ?? CancellationToken.None);
                success = true;
            }

            else
                success = await _WriterQueue.WaitAsync(timeOut.Value, cancellation ?? CancellationToken.None);
        }
        catch (OperationCanceledException ex)
        {
            // just return false
        }
        Interlocked.Decrement(ref _WritersWaiting);
        return success;
    }

    /// <summary>
    /// This method ensures that multiple read operations can occur concurrently without interfering with exclusive write operations, maintaining data integrity.<br/>
    /// When invoked, it either immediately grants access to start reading if no writers are active or waits asynchronously until reading is safe.
    /// </summary>
    /// <returns>
    /// A Success response that completes with an <see cref="IAsyncDisposable"/>.
    /// Disposing the returned object marks the reading process as complete and unlocks the ReaderWriterLock.
    /// </returns>
    /// <example>
    /// Simple usage, when no timeout / cancellation is given:
    /// <code>
    /// await using (await EnterReadAsync())
    /// {
    ///     // The shared resource(s) can be safely read within this block
    /// }
    /// </code> <br/>
    /// 
    /// </example>
    /// <example>
    /// Usage when a timeout / cancellation token is provided:
    /// <code>
    /// await using (var success = await lockObj.EnterReadAsync(TimeSpan.FromSeconds(2)))
    /// {
    ///     if (success.Success)
    ///     {
    ///         // The shared resource(s) can be safely read within this block
    ///     }
    /// }
    /// </code>
    /// </example>
    public async Task<LockAcquisitionResult> EnterReadAsync(TimeSpan? timeout = null, CancellationToken? token = null)
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
                bool success = await WaitForReaderSlot(timeout, token);
                if (!success)
                {
                    return new LockAcquisitionResult(false, async () => { });
                }
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
                bool success = await WaitForReaderSlot(timeout, token);
                if (!success)
                {
                    return new LockAcquisitionResult(false, async () => { });
                }
                Interlocked.Increment(ref _ReadersActive);
                break;
            }
            else // initialReaderWriterState == ReaderWriterState.Inactive
            {
                Interlocked.Increment(ref _ReadersActive);
                break;
            }
        }
        
        return new LockAcquisitionResult(true, async () =>
        {
            if (Interlocked.Decrement(ref _ReadersActive) == 0)
            {
                if (_WritersWaiting > 0) ReleaseWriter();
                else if (_ReaderQueue.Waiting > 0) ReleaseReaders();
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
    /// Simple usage, when no timeout / cancellation is given:
    /// <code>
    /// await using (await EnterWriteAsync())
    /// {
    ///     // The shared resource(s) can be safely written to within this block
    /// }
    /// </code> 
    /// </example>
    /// <example>
    /// Usage when a timeout / cancellation token is provided:
    /// <code>
    /// await using (var success = await lockObj.EnterWriteAsync(TimeSpan.FromSeconds(2)))
    /// {
    ///     if (success.Success)
    ///     {
    ///         // TThe shared resource(s) can be safely written to within this block
    ///     }
    /// }
    /// </code>
    /// </example>
    public async Task<LockAcquisitionResult> EnterWriteAsync(TimeSpan? timeout = null, CancellationToken? token = null)
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
                bool success = await WaitForWriterSlot(timeout, token);
                if (!success)
                {
                    return new LockAcquisitionResult(false, async () => { });
                }
                Interlocked.Exchange(ref _State, (int)ReaderWriterState.Writing);
                break;
            }
            else if (initialReaderWriterState == ReaderWriterState.Reading)
            {
                bool success = await WaitForWriterSlot(timeout, token);
                if (!success)
                {
                    return new LockAcquisitionResult(false, async () => { });
                }
                Interlocked.Exchange(ref _State, (int)ReaderWriterState.Writing);
                break;
            }
            else break;
        }

        Interlocked.Increment(ref _WritersActive);
        
        return new LockAcquisitionResult(true, async () =>
        {
            Interlocked.Decrement(ref _WritersActive);
            if (_ReaderQueue.Waiting > 0) ReleaseReaders();
            else if (_WritersWaiting > 0) ReleaseWriter();
            else Interlocked.Exchange(ref _State, (int)ReaderWriterState.Inactive);
            await Task.CompletedTask;
        });
    }
    
    /// <summary>
    /// disposable which marks the task as finished
    /// </summary>
    public class LockAcquisitionResult  : IAsyncDisposable
    {
        public bool Success { get; }
        private readonly Func<Task> _releaseAction;
        public LockAcquisitionResult (bool success, Func<Task> releaseAction)
        {
            Success = success;
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