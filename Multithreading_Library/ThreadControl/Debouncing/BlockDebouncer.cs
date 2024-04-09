using Multithreading_Library.ThreadControl.States;

namespace Multithreading_Library.ThreadControl.Debouncing;

/// <summary>
/// Provides a mechanism to ensure only the first method caller is granted right of way, while following calls are blocked off
/// until the first action completed and an optional cooldown timer expired
/// </summary>
/// <example>
/// This example shows how to use the <see cref="DebounceAsync"/> method to ensure exclusive execution of a code block:
/// <code>
/// var debouncer = new ExclusiveExecutionDebouncer(TimeSpan.FromSeconds(5));
/// if (await debouncer.Debounce())
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
///         debouncer.Unlock();
///     }
/// }
/// else
/// {
///     Console.WriteLine("Task could not execute right now.");
/// }
/// </code>
/// </example>
public class BlockDebouncer : IDisposable
{
    /// <summary>
    /// specifies how long an action is allowed to run, before the debouncer self-unlocks
    /// </summary>
    public TimeSpan? TimeOut;
    /// <summary>
    ///  specifies the minimum duration which needs to pass after the last completion before another action is allowed
    /// </summary>
    /// <remarks>
    /// actions resolve false immediately during cooldown!
    /// </remarks>
    public TimeSpan? Cooldown;
    private readonly AwaitableSignalSlim _OperationCompletedSignal = new ();
    private CancellationTokenSource _TimeoutCancellationTokenSource = new ();
    /// <summary>
    /// returns a snapshot of the last time a debounced action has completed in UTC
    /// </summary>
    public DateTime LastCompletionTime => _LastCompletionTime;
    private DateTime _LastCompletionTime = DateTime.MinValue;
    private DateTime _LastStartTime = DateTime.MinValue;
    /// <summary>
    /// returns a Snapshot of the next valid execution Time in UTC
    /// </summary>
    public DateTime NextValidExecutionTime => _NextValidExecutionTime;
    private DateTime _NextValidExecutionTime = DateTime.MinValue;
    /// <summary>
    /// Returns a snapshot of the current Execution State (idle or a debounced action is running)
    /// </summary>
    public ExecutionState ActionState => (ExecutionState)_IsRunning;
    private int _IsRunning = (int)ExecutionState.Idle;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveExecutionDebouncer"/> class.
    /// </summary>
    /// <param name="cooldownPeriod">The minimum duration to wait after the last Task has completed before allowing another execution.</param>
    /// <param name="timeOut">Specifies a maximum Duration the process is allowed to run before the Debouncer unlocks itself.</param>
    public BlockDebouncer (TimeSpan? cooldownPeriod = null, TimeSpan? timeOut = null)
    {
        TimeOut = timeOut;
        Cooldown = cooldownPeriod;
    }
    
    /// <summary>
    /// the first Debounce request resolves immediately with true<br/>
    /// further requests resolve with false as long as the first task is still running or the cooldown period is active
    /// </summary>
    /// <remarks>may return early when the set timeout for the debounced action is reached</remarks>
    /// <param name="awaitExecutionCompletion">The following tasks without right of way will wait until the debounced action is complete</param>
    /// <param name="awaitCooldownPeriod">The following tasks without right of way will wait until the cooldown period is over</param>
    /// <param name="cancellationToken">cancels the waiting for the debounced action.</param>
    /// <example>
    /// This example shows how to use the <see cref="DebounceAsync"/> method to ensure exclusive execution of a code block:
    /// <code>
    /// var debouncer = new ExclusiveExecutionDebouncer(TimeSpan.FromSeconds(5));
    /// if (await debouncer.Debounce())
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
    ///         debouncer.Unlock();
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Task could not execute right now.");
    /// }
    /// </code>
    /// </example>
    /// <returns>true when right of way was granted (action is to be executed), false when the action is to be bounced off</returns>
    public async Task<bool> DebounceAsync(
        bool awaitExecutionCompletion = false, 
        bool awaitCooldownPeriod = false,
        CancellationToken? cancellationToken = null)
    {
        if (DateTime.UtcNow < _NextValidExecutionTime) return false;
        if (Interlocked.CompareExchange(
                ref _IsRunning, 
                (int)ExecutionState.Executing, 
                (int)ExecutionState.Idle) 
            == (int)ExecutionState.Executing)
            // did not obtain right of way
        {
            if (!awaitCooldownPeriod && !awaitExecutionCompletion) return false;
            await _OperationCompletedSignal.AwaitSignalAsync(TimeOut, cancellationToken?? CancellationToken.None );
            if (awaitCooldownPeriod)
            {
                TimeSpan waitTime = _NextValidExecutionTime - DateTime.UtcNow;
                if (waitTime.TotalSeconds > 0)
                {
                    await Task.Delay(waitTime);
                }
            }
            return false;
        }
        // right of way
        _LastStartTime = DateTime.UtcNow;
        if (!TimeOut.HasValue) return true;
        _TimeoutCancellationTokenSource?.Cancel();
        _TimeoutCancellationTokenSource = new CancellationTokenSource();
        StartTimeout(_TimeoutCancellationTokenSource.Token);
        return true;
    }

    private void StartTimeout(CancellationToken cancellationToken)
    {
        if (TimeOut is null) return;
        TimeSpan elapsedTime = DateTime.UtcNow - _LastStartTime;
        Task.Delay((TimeOut.Value - elapsedTime), cancellationToken).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Unlock();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// marks the debounced task as finished
    /// </summary>
    public void Unlock()
    {
        if ((ExecutionState)Interlocked.CompareExchange(ref _IsRunning, (int)ExecutionState.Idle,
                (int)ExecutionState.Executing)
            != ExecutionState.Executing) return;
        _TimeoutCancellationTokenSource?.Cancel();
        _LastCompletionTime = DateTime.UtcNow;
        _NextValidExecutionTime = _LastCompletionTime + (Cooldown ?? TimeSpan.Zero);
        _OperationCompletedSignal.FireEvent();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ExclusiveExecutionDebouncer"/>.
    /// </summary>
    public void Dispose()
    {
        _TimeoutCancellationTokenSource?.Cancel();
        _OperationCompletedSignal.Dispose();
        _TimeoutCancellationTokenSource?.Dispose();
    }
}