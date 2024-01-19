using Xunit;
using Multithreading_Library.ThreadControl;
using System.Threading;

namespace Multithreading_Unit_Tests.ThreadControl;
public class IDSemaphoreSlimTests
{
    [Fact]
    public void ObtainLockObject_WithKey_ReturnsSemaphoreSlim()
    {
        // Arrange
        var idSemaphoreSlim = new IDSemaphoreSlim<string>();

        // Act
        var semaphore = idSemaphoreSlim.ObtainLockObject("testKey");

        // Assert
        Assert.NotNull(semaphore);
        Assert.IsType<SemaphoreSlim>(semaphore);
    }

    [Fact]
    public void ObtainLockObject_WithSameKey_ReturnsSameSemaphore()
    {
        // Arrange
        var idSemaphoreSlim = new IDSemaphoreSlim<string>();
        var firstSemaphore = idSemaphoreSlim.ObtainLockObject("testKey");

        // Act
        var secondSemaphore = idSemaphoreSlim.ObtainLockObject("testKey");

        // Assert
        Assert.Same(firstSemaphore, secondSemaphore);
    }

    [Fact]
    public void ObtainLockObject_WithInitialCount_SetsInitialCountCorrectly()
    {
        // Arrange
        var idSemaphoreSlim = new IDSemaphoreSlim<string>();
        int initialCount = 5;

        // Act
        var semaphore = idSemaphoreSlim.ObtainLockObject("testKey", initialCount);

        // Assert
        Assert.Equal(initialCount, semaphore.CurrentCount);
    }

    [Fact]
    public void ObtainLockObject_WithInitialAndMaxCount_SetsCountsCorrectly()
    {
        // Arrange
        var idSemaphoreSlim = new IDSemaphoreSlim<string>();
        int initialCount = 3;
        int maxCount = 10;

        // Act
        var semaphore = idSemaphoreSlim.ObtainLockObject("testKey", initialCount, maxCount);

        // Assert
        semaphore.Release(7); // Release 7 times to test max count
        Assert.Throws<SemaphoreFullException>(() => semaphore.Release());
    }
}