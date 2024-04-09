using Multithreading_Library.ThreadControl.States;

namespace Multithreading_Library.ThreadControl;

/// <summary>
/// provides functionality to subscribe to a signal which can be awaited.<br/>
/// a subscription is only valid for one cycle.
/// </summary>
public class AwaitableSignalSlim
{
    private readonly SemaphoreSlim _WaitContext = new SemaphoreSlim(0, int.MaxValue);
    /// <summary>
    /// returns a snapshot of the subscribed / waiting count
    /// </summary>
    public int Waiting => _Waiting;
    private int _Waiting = 0;
    /// <summary>
    /// returns a snapshot of the execution state
    /// </summary>
    public ExecutionState Status => (ExecutionState)_Status;
    private int _Status = 0;

    /// <summary>
    /// returns an awaitable Task that completes then the event is either fired or failed (timeout / cancelled)
    /// </summary>
    /// <param name="timeOut">the maximum timespan to wait for the event</param>
    /// <param name="cancellation">cancels the waiting for the event</param>
    /// <returns>true when the event was fired, false when not</returns>
    public async Task<bool> AwaitSignalAsync(TimeSpan? timeOut, CancellationToken? cancellation)
    {
        if ((ExecutionState)Interlocked.CompareExchange(ref _Status, 0, 0) == ExecutionState.Executing) return true;
        Interlocked.Increment(ref _Waiting);
        bool eventFired = false;
        try
        {
            if (timeOut is null)
            {
                await _WaitContext.WaitAsync(cancellation ?? CancellationToken.None);
                eventFired = true;
            }

            else
                eventFired = await _WaitContext.WaitAsync(timeOut.Value, cancellation ?? CancellationToken.None);
        }
        catch (TaskCanceledException ex)
        {
            // do nothing, the task was cancelled eventFired = false;
        }

        if (!eventFired)
        {
            if (Interlocked.Decrement(ref _Waiting) != 0) return eventFired;
            // Lock releases were calculated already!
            Interlocked.Increment(ref _Waiting);
            await _WaitContext.WaitAsync();
            if (_WaitContext.CurrentCount == 0) Interlocked.Exchange(ref _Status, (0));
            eventFired = true;
        }
        else
        {
            if (_WaitContext.CurrentCount == 0)
                Interlocked.Exchange(ref _Status, 0);
        }

        return eventFired;
    }

    /// <summary>
    /// fires the event, releasing all subscribed Waiters
    /// </summary>
    public void FireEvent()
    {
        if ((ExecutionState)Interlocked.CompareExchange(ref _Status, 1, 0) == ExecutionState.Executing)
            return;

        // Release all waiting tasks.
        int toRelease = Interlocked.Exchange(ref _Waiting, 0);
        if (toRelease > 0)
        {
            _WaitContext.Release(toRelease);
        }
    }
}