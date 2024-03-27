using System;
using System.Threading.Tasks;
using Xunit;
using Multithreading_Library.ThreadControl.Debouncing;

namespace Multithreading_Unit_Tests.ThreadControl.Debouncing;

public class BlockDebouncerTests
{
    [Fact]
    public void TryExecute_AllowsFirstExecutionImmediately()
    {
        // Arrange
        var debouncer = new BlockDebouncer(TimeSpan.FromSeconds(1));
        bool actionExecuted = false;

        // Act
        var result = debouncer.TryExecute(() => actionExecuted = true);

        // Assert
        Assert.True(result, "The first execution should be allowed.");
        Assert.True(actionExecuted, "The action should have been executed.");
    }

    [Fact]
    public async Task TryExecute_BlocksSubsequentExecutionsWithinCooldownPeriod()
    {
        // Arrange
        var debouncer = new BlockDebouncer(TimeSpan.FromSeconds(1));
        int executionCount = 0;

        // Act
        debouncer.TryExecute(() => executionCount++);
        await Task.Delay(500); // Wait for half the cooldown period
        debouncer.TryExecute(() => executionCount++);

        // Assert
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task TryExecute_AllowsExecutionAfterCooldownPeriod()
    {
        // Arrange
        var debouncer = new BlockDebouncer(TimeSpan.FromMilliseconds(100)); // Short cooldown for testing
        int executionCount = 0;

        // Act
        debouncer.TryExecute(() => executionCount++);
        await Task.Delay(150); // Wait longer than the cooldown period
        var result = debouncer.TryExecute(() => executionCount++);

        // Assert
        Assert.True(result, "The second execution should be allowed after the cooldown period.");
        Assert.Equal(2, executionCount);
    }

    [Fact]
    public void TryExecute_ReturnsFalseIfBlocked()
    {
        // Arrange
        var debouncer = new BlockDebouncer(TimeSpan.FromSeconds(1));

        // Act & Assert
        Assert.True(debouncer.TryExecute(() => { }), "The first call should succeed.");
        Assert.False(debouncer.TryExecute(() => { }), "The second call should be blocked and return false.");
    }
}