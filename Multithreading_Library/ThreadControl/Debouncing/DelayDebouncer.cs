namespace Multithreading_Library.ThreadControl.Debouncing;

/// <summary>
/// Provides a thread-safe mechanism to delay the execution of an action until a specified period of inactivity has passed.
/// This is particularly useful for reducing the frequency of operations that shouldn't be executed repeatedly or parallel in a short period
/// (e.g., saving data to disk after changes). The debouncer ensures that only the last action is executed if multiple requests
/// are made within the delay period. This class is safe for use in multi-threaded environments.
/// </summary>
/// <example>
/// This example demonstrates how to use the <see cref="DelayDebouncer"/> to debounce a method that updates a file.
/// The debouncer is configured with a 2-second delay, meaning that the update will only occur if 2 seconds have passed without
/// any further calls to the debouncer.<br/>
/// <code>
/// public class FileUpdater
/// {
///     private ThreadSafeDelayDebouncer _debouncer = new DelayDebouncer(TimeSpan.FromSeconds(2));
///     
///     public void UpdateFile(string content)
///     {
///         _debouncer.Debounce(() =>
///         {
///             File.WriteAllText("path/to/file.txt", content);
///             Console.WriteLine("File updated");
///         });
///     }
/// }
/// </code>
/// </example>
public class DelayDebouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private Action? _pendingAction;
    private readonly object _lock = new object();
    private readonly ManualResetEventSlim _resetEvent = new (true); // Initially set to signaled state.
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayDebouncer"/> class with a specified delay.
    /// </summary>
    /// <param name="delay">The delay to wait before invoking the action. This defines the period of inactivity required
    /// before the action is executed. If another request is made within this period, the delay is reset.</param>
    public DelayDebouncer(TimeSpan delay)
    {
        _delay = delay;
    }

    /// <summary>
    /// Debounces the given action. This method schedules the action to be executed after a delay period has passed
    /// without any further calls to this method. If called multiple times within the delay period, only the last call's
    /// action is executed, ensuring that the action is only performed once after the last request.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and ensures that concurrent calls are managed in a way that prevents race conditions,
    /// with each call correctly resetting the delay period. It's suitable for scenarios where high-frequency events (e.g.,
    /// user input, IO operations) need to be managed efficiently by reducing the execution frequency of an associated action.
    /// </remarks>
    /// <example>
    /// This example demonstrates how to use the <see cref="DelayDebouncer"/> to debounce a method that updates a file.
    /// The debouncer is configured with a 2-second delay, meaning that the update will only occur if 2 seconds have passed without
    /// any further calls to the debouncer.<br/>
    /// <code>
    /// public class FileUpdater
    /// {
    ///     private ThreadSafeDelayDebouncer _debouncer = new DelayDebouncer(TimeSpan.FromSeconds(2));
    ///     
    ///     public void UpdateFile(string content)
    ///     {
    ///         _debouncer.Debounce(() =>
    ///         {
    ///             File.WriteAllText("path/to/file.txt", content);
    ///             Console.WriteLine("File updated");
    ///         });
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <param name="action">The action to debounce. This action is executed once the delay period has elapsed without any
    /// further calls to this method. The action is executed in the context of a ThreadPool thread.</param>
    public void Debounce(Action action)
    {
        lock (_lock)
        {
            _pendingAction = action;
            if (!_resetEvent.IsSet)
            {
                // If a pending action is already scheduled, we just update the action and let the existing task handle it.
                return;
            }

            _resetEvent.Reset(); // Reset the event to non-signaled, indicating a pending action.
        }

        Task.Run(() =>
        {
            try
            {
                // Use a local cancellation token source that can be canceled if a new action is debounced.
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token))
                {
                    Action? localAction;

                    if (linkedCts.Token.WaitHandle.WaitOne(_delay))
                    {
                        // If we get here, the wait was cancelled.
                        return;
                    }

                    lock (_lock)
                    {
                        localAction = _pendingAction;
                        _pendingAction = null;
                        _resetEvent.Set(); // No longer any pending action.
                    }

                    localAction?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected if a new action is debounced before the delay period passes.
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Cancel any pending debounced actions. This will cause the waiting task to complete early.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource(); // Prepare for next action.
        _resetEvent.Set(); // Ensure any lock is released.
    }

    /// <summary>
    /// disposes internal resources (cancellation token source) when the instance is no longer needed
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// disposes internal resources (cancellation token source) when the instance is no longer needed
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        Cancel();
        if (disposing)
        {
            _cancellationTokenSource.Dispose();
            _resetEvent.Dispose();
        }

        _disposed = true;
    }

    ~DelayDebouncer()
    {
        Dispose(false);
    }
}