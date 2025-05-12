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
        var ctsField = typeof(DelayDebouncer).GetField("_debounceCts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(ctsField);
        debouncer.Dispose();

        var ctsAfterDispose = (CancellationTokenSource)ctsField!.GetValue(debouncer)!;
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var _ = ctsAfterDispose.Token;
        });
    }

    [Fact]
    public async Task DebounceAsync_ExecutesActionAfterDelay()
    {
        int executionCount = 0;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(100));

        debouncer.DebounceAsync(async () =>
        {
            await Task.Delay(10); // Simulate async work.
            Interlocked.Increment(ref executionCount);
        });

        await Task.Delay(50);
        Assert.Equal(0, executionCount); // Action should not have executed yet.

        await Task.Delay(100);
        Assert.Equal(1, executionCount); // Action should have executed after delay.

        debouncer.Dispose();
    }

    [Fact]
    public async Task DebounceAsync_ExecutesOnlyLastAction()
    {
        int executionCount = 0;
        int executedAction = -1;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(100));

        // Schedule multiple async actions in quick succession.
        for (int i = 0; i < 5; i++)
        {
            int localI = i;
            debouncer.DebounceAsync(async () =>
            {
                await Task.Delay(10); // Simulate async work.
                Interlocked.Increment(ref executionCount);
                executedAction = localI;
            });
        }

        await Task.Delay(200);
        Assert.Equal(1, executionCount); // Only the last action should have executed.
        Assert.Equal(4, executedAction); // The last scheduled action should have executed.

        debouncer.Dispose();
    }

    [Fact]
    public async Task DebounceAsync_CancellationPreventsAction()
    {
        int executionCount = 0;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(1000));

        debouncer.DebounceAsync(async () =>
        {
            await Task.Delay(10);
            Interlocked.Increment(ref executionCount);
        });

        debouncer.Cancel();

        await Task.Delay(150);
        Assert.Equal(0, executionCount); // Action should have been canceled.

        debouncer.Dispose();
    }

    [Fact]
    public void DisposeAsync_ReleasesResources()
    {
        var debouncer = new DelayDebouncer(TimeSpan.FromSeconds(1));
        var ctsField = typeof(DelayDebouncer).GetField("_debounceCts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(ctsField);
        debouncer.Dispose();

        var ctsAfterDispose = (CancellationTokenSource)ctsField!.GetValue(debouncer)!;
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var _ = ctsAfterDispose.Token;
        });
    }
    [Fact]
    public async Task Debounce_FixedDelay_DoesNotResetTimer()
    {
        int executionCount = 0;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(100), DelayDebouncer.DebounceMode.FixedDelay);

        debouncer.Debounce(() => Interlocked.Increment(ref executionCount));

        // Wait half the delay and issue another call
        await Task.Delay(50);
        debouncer.Debounce(() => Interlocked.Increment(ref executionCount)); // Should be ignored

        // Wait beyond the original delay
        await Task.Delay(100);

        Assert.Equal(1, executionCount); // Should execute only once (after first call)
        debouncer.Dispose();
    }
    [Fact]
    public async Task Debounce_Deferred_ResetsTimer()
    {
        int executionCount = 0;
        var debouncer = new DelayDebouncer(TimeSpan.FromMilliseconds(100), DelayDebouncer.DebounceMode.Deferred);

        debouncer.Debounce(() => Interlocked.Increment(ref executionCount));
        await Task.Delay(80); // Halfway through delay

        debouncer.Debounce(() => Interlocked.Increment(ref executionCount)); // Resets delay
        await Task.Delay(80); // Not yet enough for second delay
        Assert.Equal(0, executionCount); // Should still not have executed

        await Task.Delay(2000); // Now it should have fired
        Assert.Equal(1, executionCount); // Should now have executed

        debouncer.Dispose();
    }


}