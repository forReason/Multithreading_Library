using System;
using System.Threading.Tasks;
using Multithreading_Library.ThreadControl.Debouncing;
using Xunit;

namespace Multithreading_Unit_Tests.ThreadControl.Debouncing;

public class ExclusiveExecutionDebouncerTests
{
    [Fact]
    public async Task Debounce_EnsuresExclusiveExecution()
    {
        var debouncer = new ExclusiveExecutionDebouncer();
        bool firstTaskCanExecute = await debouncer.Debounce(wait: false);

        Assert.True(firstTaskCanExecute); // First task should acquire the lock

        bool secondTaskCanExecute = await debouncer.Debounce(wait: false);

        // Second task should not acquire the lock as the first one has not released it yet
        Assert.False(secondTaskCanExecute);
        
        debouncer.Dispose(); // Cleanup
    }

    [Fact]
    public async Task Debounce_WithWait_WaitsForRelease()
    {
        var debouncer = new ExclusiveExecutionDebouncer();
        var firstTask = debouncer.Debounce(wait: false);
        
        var secondTask = Task.Run(async () => await debouncer.Debounce(wait: true));

        // Delay to ensure the second task starts and gets into the waiting state
        await Task.Delay(50); 
        
        Assert.True(await firstTask); // First task should acquire the lock
        Assert.True(firstTask.IsCompleted);
        Assert.False(secondTask.IsCompleted);
        debouncer.ReleaseLock(); // This should allow the second task to complete
        Assert.False(await secondTask); // Second task waits and then gets false
        Assert.True(secondTask.IsCompleted);
        // Cleanup
        debouncer.Dispose();
    }

    [Fact]
    public async Task Debounce_ReleasesAfterTimeout()
    {
        var shortTimeout = TimeSpan.FromMilliseconds(100);
        var debouncer = new ExclusiveExecutionDebouncer(shortTimeout);

        bool firstTaskCanExecute = await debouncer.Debounce(wait: false);
        Assert.True(firstTaskCanExecute);

        // Wait for longer than the timeout to ensure the lock is released
        await Task.Delay(shortTimeout + TimeSpan.FromMilliseconds(50));

        // Try again after the timeout
        bool secondTaskCanExecute = await debouncer.Debounce(wait: false);
        Assert.True(secondTaskCanExecute); // Should be true as the timeout should have released the lock

        // Cleanup
        debouncer.Dispose();
    }
}