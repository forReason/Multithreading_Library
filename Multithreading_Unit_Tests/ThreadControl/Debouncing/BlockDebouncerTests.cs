using System;
using System.Threading.Tasks;
using Xunit;
using Multithreading_Library.ThreadControl.Debouncing;

namespace Multithreading_Unit_Tests.ThreadControl.Debouncing;

public class BlockDebouncerTests
{
    [Fact]
    public async Task DebounceAsync_WithoutConcurrency_AllowsImmediateExecution()
    {
        var debouncer = new BlockDebouncer();
        bool result = await debouncer.DebounceAsync();
        
        Assert.True(result);
    }

    [Fact]
    public async Task DebounceAsync_WithCooldown_PreventsImmediateReexecution()
    {
        var cooldown = TimeSpan.FromMilliseconds(200);
        var debouncer = new BlockDebouncer(cooldownPeriod: cooldown);
        
        bool firstResult = await debouncer.DebounceAsync();
        await Task.Delay(50); // Shorter than cooldown
        debouncer.Unlock(); // Simulate end of first execution
        bool secondResult = await debouncer.DebounceAsync();

        Assert.True(firstResult);
        Assert.False(secondResult);
    }

    [Fact]
    public async Task DebounceAsync_WithTimeout_AllowsReexecutionAfterTimeout()
    {
        var timeout = TimeSpan.FromMilliseconds(100);
        var debouncer = new BlockDebouncer(timeOut: timeout);
        
        bool firstResult = await debouncer.DebounceAsync();
        await Task.Delay(timeout + TimeSpan.FromMilliseconds(50)); // Wait for timeout to expire
        bool secondResult = await debouncer.DebounceAsync();

        Assert.True(firstResult);
        Assert.True(secondResult);
    }

    [Fact]
    public async Task DebounceAsync_WithCooldownAndAwaitCooldownPeriod_WaitsForCooldown()
    {
        var cooldown = TimeSpan.FromMilliseconds(200);
        var debouncer = new BlockDebouncer(cooldownPeriod: cooldown);
        
        bool firstResult = await debouncer.DebounceAsync();
        Task<bool> debounceTask = debouncer.DebounceAsync(awaitCooldownPeriod: true);
        
        debouncer.Unlock(); // End of first execution
        var startTime = DateTime.Now;
        bool secondResult = await debounceTask;
        var elapsed = DateTime.Now - startTime;
        Assert.True(firstResult);
        
        Assert.False(secondResult);
        Assert.True(elapsed < cooldown*1.1, $"Elapsed time {elapsed} should be smaller than {cooldown*1.1}");
        Assert.True(elapsed > cooldown*0.9, $"Elapsed time {elapsed} should be greater than {cooldown*0.9}");
    }

    [Fact]
    public async Task Unlock_UpdatesLastCompletionTime()
    {
        var debouncer = new BlockDebouncer();
        await debouncer.DebounceAsync();
        
        debouncer.Unlock();
        var lastCompletionTime = debouncer.LastCompletionTime;

        Assert.True(DateTime.UtcNow >= lastCompletionTime, "Last completion time should be updated to a recent time after unlock.");
    }

    [Fact]
    public void Dispose_CancelsPendingTimeoutTask()
    {
        var timeout = TimeSpan.FromSeconds(1);
        var debouncer = new BlockDebouncer(timeOut: timeout);

        var task = debouncer.DebounceAsync();

        debouncer.Dispose();

        // Test passes if no unobserved task exception occurs due to the pending timeout task being properly cancelled and disposed.
        Assert.True(task.IsCompletedSuccessfully);
    }
}