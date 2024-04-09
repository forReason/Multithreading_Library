using System;
using System.Threading;
using System.Threading.Tasks;
using Multithreading_Library.ThreadControl;
using Xunit;

namespace Multithreading_Unit_Tests.ThreadControl;

public class AwaitableSignalSlimTests
{
    // TODO: TEST
    [Fact]
    public async Task AwaitSignalAsync_SignalNotFired_TimesOut()
    {
        var signal = new AwaitableSignalSlim();
        bool result = await signal.AwaitSignalAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task AwaitSignalAsync_SignalFired_Succeeds()
    {
        var signal = new AwaitableSignalSlim();
        var task = Task.Run(async () => await signal.AwaitSignalAsync(null, CancellationToken.None));

        // Wait a moment to ensure the task is awaiting the signal
        await Task.Delay(50);
        signal.FireEvent();

        bool result = await task;
        Assert.True(result);
    }

    [Fact]
    public async Task AwaitSignalAsync_MultipleWaiters_AllReleased()
    {
        var signal = new AwaitableSignalSlim();
        var tasks = new Task<bool>[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = signal.AwaitSignalAsync(null, CancellationToken.None);
        }

        // Ensure all tasks are waiting
        await Task.Delay(50);
        signal.FireEvent();

        var results = await Task.WhenAll(tasks);
        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task AwaitSignalAsync_WithCancellation_TokenIsRespected()
    {
        var signal = new AwaitableSignalSlim();
        using var cts = new CancellationTokenSource();

        var task = signal.AwaitSignalAsync(null, cts.Token);
        
        // Cancel before firing the signal
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task FireEvent_WithNoWaiters_DoesNotThrow()
    {
        var signal = new AwaitableSignalSlim();
        
        // This should not throw
        var exceptionRecorded = Record.Exception(() => signal.FireEvent());
        
        Assert.Null(exceptionRecorded);
    }
    [Fact]
    public async Task StressTest_ConcurrentAwaitAndFire()
    {
        var signal = new AwaitableSignalSlim();
        int waitersCount = 1000;
        var tasks = new Task<bool>[waitersCount];

        for (int i = 0; i < waitersCount; i++)
        {
            tasks[i] = signal.AwaitSignalAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }

        for (int j = 0; j < 10; j++) // Fire event multiple times during the wait period.
        {
            await Task.Delay(1000); // Delay to simulate work and give time for all tasks to start waiting.
            signal.FireEvent();
        }

        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            Assert.True(task.Result); // Each task should have been completed due to the event being fired.
        }
    }
}