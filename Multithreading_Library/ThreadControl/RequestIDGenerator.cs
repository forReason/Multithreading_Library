/// <summary>
/// Provides thread-safe generation of unique request IDs.
/// </summary>
/// <remarks>
/// Performs a threadsafe rollover at int.MaxValue
/// </remarks>
public class RequestIDGenerator
{
    private int _nextRequestId;

    /// <summary>
    /// Initializes a new instance of the RequestIDGenerator class.
    /// </summary>
    public RequestIDGenerator()
    {
        _nextRequestId = 0;
    }

    /// <summary>
    /// Generates a unique request ID for each WebSocket request. <br/><br/>
    /// 
    /// Performs a threadsafe rollover at int.MaxValue<br/><br/>
    /// This method ensures that the request ID is always positive
    /// and handles the rollover safely when the ID reaches Int32.MaxValue. The method uses an atomic operation to prevent race conditions
    /// in a multi-threaded environment.
    /// </summary>
    /// <remarks>
    /// The method uses a loop with the `Interlocked.CompareExchange` method to handle the increment and rollover atomically.
    /// This ensures that even in a highly concurrent scenario, the request IDs remain unique and consistent.
    ///<br/><br/>
    /// The process works as follows:<br/>
    /// 1. It captures the current value of the _nextRequestId.<br/>
    /// 2. Calculates the new value, which is either the incremented value of the current request ID or 1 if the current value
    ///    has reached Int32.MaxValue (to handle rollover).<br/>
    /// 3. Uses `Interlocked.CompareExchange` to atomically set the _nextRequestId to the new value if the current _nextRequestId
    ///    has not changed since the initial read.<br/>
    /// 4. If another thread has modified _nextRequestId in the meantime, the loop repeats this process with the updated value.
    ///<br/><br/>
    /// This approach avoids locking, thus preventing the performance degradation in environments with high concurrency.
    /// </remarks>
    /// <returns>A unique, positive request ID that is safe for use in concurrent scenarios.</returns>
    public int GetNextRequestId()
    {
        int original, newValue;
        do
        {
            original = _nextRequestId;
            newValue = original == Int32.MaxValue ? 1 : original + 1;
        } while (Interlocked.CompareExchange(ref _nextRequestId, newValue, original) != original);

        return newValue;
    }
}
