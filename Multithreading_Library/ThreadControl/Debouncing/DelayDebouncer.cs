namespace Multithreading_Library.ThreadControl.Debouncing;

/// <summary>
/// Provides a thread-safe mechanism to delay the execution of an action until a specified period of inactivity has passed.
/// This is particularly useful for reducing the frequency of operations that shouldn't be executed repeatedly or parallel in a short period
/// (e.g., saving data to disk after changes). The debouncer ensures that only the last action is executed if multiple requests
/// are made within the delay period. This class is safe for use in multi-threaded environments.
/// </summary>
/// <remarks>
/// DebounceMode steers whether the action is always executed after the timer runs out or deferred until no more calls happen.
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
public class DelayDebouncer : IDisposable
{
    private readonly TimeSpan _delay;
    private Action? _pendingAction;
    private Func<Task>? _pendingAsyncAction;
    private readonly object _lock = new object();
    private readonly ManualResetEventSlim _resetEvent = new(true);
    private CancellationTokenSource _debounceCts = new CancellationTokenSource();
    private readonly DebounceMode _mode;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayDebouncer"/> class with a specified delay.
    /// </summary>
    /// <param name="delay">The delay to wait before invoking the action.
    /// This defines the period of inactivity required before the action is executed.
    /// If another request is made within this period, the delay is reset.</param>
    /// <param name="mode">steers whether the action is always executed after the timer runs out or
    /// deferred until no more calls happen.</param>
    public DelayDebouncer(TimeSpan delay, DebounceMode mode = DebounceMode.FixedDelay)
    {
        _delay = delay;
        _mode = mode;
    }

    /// <summary>
    /// Debounces the given action. This method schedules the action to be executed after a delay period has passed.
    /// When Mode is set to FixedDely, the method is always called after a certain time.
    /// When Mode is set to Deferred, execution can stall infinitely, until no more calls happen.
    /// If called multiple times within the delay period, only the last call's.
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
            _pendingAsyncAction = null;

            if (_mode == DebounceMode.Deferred)
            {
                _debounceCts.Cancel();
                _debounceCts.Dispose();
                _debounceCts = new CancellationTokenSource();
                _resetEvent.Set(); // Ensure we allow a new task to start
            }

            if (!_resetEvent.IsSet && _mode != DebounceMode.Deferred)
                return;

            _resetEvent.Reset();
        }

        Task.Run(() => ExecuteDebouncedActionAsync(_debounceCts.Token));
    }



    public void DebounceAsync(Func<Task> asyncAction)
    {
        lock (_lock)
        {
            _pendingAsyncAction = asyncAction;
            _pendingAction = null;

            if (_mode == DebounceMode.Deferred)
            {
                _debounceCts.Cancel();
                _debounceCts.Dispose();
                _debounceCts = new CancellationTokenSource();
                _resetEvent.Set(); // allow task restart
            }

            if (!_resetEvent.IsSet && _mode != DebounceMode.Deferred)
                return;

            _resetEvent.Reset();
        }

        Task.Run(() => ExecuteDebouncedActionAsync(_debounceCts.Token));
    }



    private async Task ExecuteDebouncedActionAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_delay, token);
        }
        catch (TaskCanceledException)
        {
            return; // Timer was reset
        }

        Action? localAction;
        Func<Task>? localAsyncAction;

        lock (_lock)
        {
            localAction = _pendingAction;
            localAsyncAction = _pendingAsyncAction;
            _pendingAction = null;
            _pendingAsyncAction = null;
            _resetEvent.Set();
        }

        if (localAsyncAction != null)
            await localAsyncAction();
        else
            localAction?.Invoke();
    }

    /// <summary>
    /// Cancel any pending debounced actions. This will cause the waiting task to complete early.
    /// </summary>
    public void Cancel()
    {
        _debounceCts.Cancel();
        _debounceCts = new CancellationTokenSource();
        _resetEvent.Set();
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
            _debounceCts.Dispose();
            _resetEvent.Dispose();
        }
        _disposed = true;
    }

    ~DelayDebouncer()
    {
        Dispose(false);
    }
    public enum DebounceMode
    {
        FixedDelay,   // Trigger once after the timer starts, ignore further calls
        Deferred      // Restart timer on every call
    }
}