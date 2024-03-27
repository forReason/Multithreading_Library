using System;
using System.Threading;
using System.Threading.Tasks;
using Multithreading_Library.ThreadControl.Debouncing;
using Xunit;

namespace Multithreading_Unit_Tests.ThreadControl.Debouncing;

public class DelayDebouncerTests
{
    [Fact]
    public async Task Debounce_ExecutesActionAfterDelay()
    {
        int executionCount = 0;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(100));
        debouncer.Debounce(() => executionCount++);

        await Task.Delay(50);
        Assert.Equal(0, executionCount); // Action should not have executed yet.

        await Task.Delay(100);
        Assert.Equal(1, executionCount); // Action should have executed once.

        debouncer.Dispose();
    }

    [Fact]
    public async Task Debounce_ExecutesOnlyLastAction()
    {
        int executionCount = 0;
        int executedAction = -1;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(100));

        // Schedule multiple actions in quick succession.
        for (int i = 0; i < 5; i++)
        {
            int localI = i;
            debouncer.Debounce(() =>
            {
                Interlocked.Increment(ref executionCount);
                executedAction = localI;
            });
            //await Task.Delay(10);
        }

        await Task.Delay(150);
        Assert.Equal(1, executionCount); // Only the last action should have executed.
        Assert.Equal(4, executedAction); // Only the last action should have executed.

        debouncer.Dispose();
    }

    [Fact]
    public async Task Debounce_CancellationPreventsAction()
    {
        int executionCount = 0;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(1000));
        debouncer.Debounce(() => executionCount++);
        debouncer.Cancel();

        await Task.Delay(150);
        Assert.Equal(0, executionCount); // Action should not have executed.

        debouncer.Dispose();
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        var debouncer = new DelayDebouncer(TimeSpan.FromSeconds(1));
        var ctsField = typeof(DelayDebouncer).GetField("_cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(ctsField);
        debouncer.Dispose();

        var ctsAfterDispose = (CancellationTokenSource)ctsField!.GetValue(debouncer)!;
        // Correct usage of Assert.Throws to check for ObjectDisposedException when accessing the CancellationToken.
        Assert.Throws<ObjectDisposedException>(() => { var _ = ctsAfterDispose.Token; }); // Indicates Dispose was called.
    }

}