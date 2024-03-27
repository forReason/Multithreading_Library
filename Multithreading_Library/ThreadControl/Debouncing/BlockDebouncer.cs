namespace Multithreading_Library.ThreadControl.Debouncing;

/// <summary>
/// Block debouncer lets the first thread enter.<br/>
/// Further threads are blocked from parallel execution and immediately return<br/>
/// optionally define a block period delay which new threads have to wait because the action can be executed again
/// </summary>
/// <example>
/// This example shows how to use the <see cref="BlockDebouncer"/> to prevent rapid consecutive executions of a method.
/// <code>
/// class Program
/// {
///     static void Main(string[] args)
///     {
///         var debouncer = new BlockDebouncer(TimeSpan.FromSeconds(2));
///         
///         // Simulate multiple concurrent calls
///         Parallel.For(0, 10, i =>
///         {
///             if (debouncer.TryExecute(() => Console.WriteLine($"Executed at: {DateTime.UtcNow:O}")))
///             {
///                 Console.WriteLine($"Execution {i} started.");
///             }
///             else
///             {
///                 Console.WriteLine($"Execution {i} blocked.");
///             }
///         });
///         
///         Console.ReadLine();
///     }
/// }
/// </code>
/// In this example, multiple concurrent calls attempt to execute through the debouncer. Due to the debouncer's logic,
/// only the first call is executed immediately, and subsequent calls within the next 2 seconds are blocked. This ensures
/// that the action is not executed more frequently than once every 2 seconds.
/// </example>
public class BlockDebouncer
{
    private readonly TimeSpan? _cooldownPeriod;
    private DateTime _lastExecutionTime = DateTime.MinValue;
    private DateTime _nextValidExecutionTime = DateTime.MinValue;
    private bool _isExecuting;
    private readonly object _lock = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockDebouncer"/> class.
    /// </summary>
    /// <param name="cooldownPeriod">The duration to wait before allowing another execution after the first execution.</param>
    public BlockDebouncer(TimeSpan? cooldownPeriod)
    {
        _cooldownPeriod = cooldownPeriod;
    }

    /// <summary>
    /// Attempts to execute the provided action. If an action is currently executing
    /// or the block period has not elapsed since the last execution, the call is ignored.
    /// </summary>
    /// <example>
    /// This example shows how to use the <see cref="BlockDebouncer"/> to prevent rapid consecutive executions of a method.
    /// <code>
    /// class Program
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         var debouncer = new BlockDebouncer(TimeSpan.FromSeconds(2));
    ///         
    ///         // Simulate multiple concurrent calls
    ///         Parallel.For(0, 10, i =>
    ///         {
    ///             if (debouncer.TryExecute(() => Console.WriteLine($"Executed at: {DateTime.UtcNow:O}")))
    ///             {
    ///                 Console.WriteLine($"Execution {i} started.");
    ///             }
    ///             else
    ///             {
    ///                 Console.WriteLine($"Execution {i} blocked.");
    ///             }
    ///         });
    ///         
    ///         Console.ReadLine();
    ///     }
    /// }
    /// </code>
    /// In this example, multiple concurrent calls attempt to execute through the debouncer. Due to the debouncer's logic,
    /// only the first call is executed immediately, and subsequent calls within the next 2 seconds are blocked. This ensures
    /// that the action is not executed more frequently than once every 2 seconds.
    /// </example>
    /// <param name="action">The action to execute.</param>
    /// <returns>a boolean indicating if an action has been started or not (no indicator for execution success)</returns>
    public bool TryExecute(Action action)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (_isExecuting || now < _nextValidExecutionTime)
            {
                // Action is currently executing or cooldown period has not elapsed.
                return false;
            }

            _isExecuting = true;
        }

        try
        {
            // Execute the action outside of the lock to prevent blocking other threads.
            action();
        }
        finally
        {
            lock (_lock)
            {
                _lastExecutionTime = DateTime.UtcNow;
                _nextValidExecutionTime = _lastExecutionTime + (_cooldownPeriod ?? TimeSpan.Zero);
                _isExecuting = false;
            }
        }

        return true;
    }
}