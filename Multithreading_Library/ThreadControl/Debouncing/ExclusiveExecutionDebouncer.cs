namespace Multithreading_Library.ThreadControl.Debouncing;

/// <summary>
/// Provides a mechanism to ensure exclusive execution of a code block with support for optional waiting and timeouts.
/// </summary>
/// <remarks>
/// This class uses a combination of a <see cref="SemaphoreSlim"/> and <see cref="ManualResetEventSlim"/>
/// to manage exclusive access and waiting functionality. It supports optional timeouts to automatically
/// release the lock after a specified period.
/// </remarks><example>
/// This example shows how to use the <see cref="Debounce"/> method to ensure exclusive execution of a code block:
/// <code>
/// var debouncer = new ExclusiveExecutionDebouncer(TimeSpan.FromSeconds(5));
/// if (await debouncer.Debounce(wait: false))
/// {
///     try
///     {
///         Console.WriteLine("Task is executing.");
///         // Simulate work by delaying for a short period
///         await Task.Delay(1000);
///     }
///     finally
///     {
///         // Ensure the debouncer is released when the task is done
///         debouncer.ReleaseLock();
///     }
/// }
/// else
/// {
///     Console.WriteLine("Task could not execute right now.");
/// }
/// </code>
/// </example>
public class ExclusiveExecutionDebouncer : IDisposable
{
    private readonly SemaphoreSlim _lock = new (1, 1);
    private readonly ManualResetEventSlim _operationCompleted = new (true);
    private readonly TimeSpan? _timeout;
    private CancellationTokenSource _timeoutCancellationTokenSource = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveExecutionDebouncer"/> class.
    /// </summary>
    /// <param name="timeout">The optional timeout after which the lock is automatically released.</param>
    public ExclusiveExecutionDebouncer(TimeSpan? timeout = null)
    {
        _timeout = timeout;
    }

    /// <summary>
    /// Attempts to acquire the right to execute the code block exclusively.
    /// </summary>
    /// <param name="wait">If true, the caller waits for the current operation to complete before determining it cannot proceed.</param>
    /// <returns>True if the caller acquires the right to execute, otherwise false.</returns>
    /// <remarks>
    /// Callers that are willing to wait but find another operation in progress will wait for it to complete
    /// and then receive a false return value, indicating that they should not proceed.
    /// </remarks>
    /// <example>
    /// This example shows how to use the <see cref="Debounce"/> method to ensure exclusive execution of a code block:
    /// <code>
    /// var debouncer = new ExclusiveExecutionDebouncer(TimeSpan.FromSeconds(5));
    /// if (await debouncer.Debounce(wait: false))
    /// {
    ///     try
    ///     {
    ///         Console.WriteLine("Task is executing.");
    ///         // Simulate work by delaying for a short period
    ///         await Task.Delay(1000);
    ///     }
    ///     finally
    ///     {
    ///         // Ensure the debouncer is released when the task is done
    ///         debouncer.ReleaseLock();
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Task could not execute right now.");
    /// }
    /// </code>
    /// </example>
    public async Task<bool> Debounce(bool wait = false)
    {
        if (!await _lock.WaitAsync(0))
        {
            if (!wait) return false;
            // Wait for the operation to complete instead of acquiring the semaphore
            _operationCompleted.Wait();
            return false;
        }

        _operationCompleted.Reset(); // Reset the event to block waiting threads

        if (!_timeout.HasValue) return true;
        _timeoutCancellationTokenSource?.Cancel();
        _timeoutCancellationTokenSource = new CancellationTokenSource();
        StartTimeout(_timeoutCancellationTokenSource.Token);

        return true;
    }

    private void StartTimeout(CancellationToken cancellationToken)
    {
        if (_timeout is null) return;
        Task.Delay(_timeout.Value, cancellationToken).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                ReleaseLock();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// marks the debounced task as finished
    /// </summary>
    public void ReleaseLock()
    {
        _timeoutCancellationTokenSource?.Cancel();
        _lock.Release();
        _operationCompleted.Set(); // Signal waiting threads that the operation is complete
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ExclusiveExecutionDebouncer"/>.
    /// </summary>
    public void Dispose()
    {
        _operationCompleted.Set();
        _timeoutCancellationTokenSource?.Cancel();
        _lock.Dispose();
        _operationCompleted.Dispose();
        _timeoutCancellationTokenSource?.Dispose();
    }
}