using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class RequestIDGeneratorTests
{
    [Fact]
    public void GetNextRequestId_ReturnsUniqueIds()
    {
        var generator = new RequestIDGenerator();
        var idSet = new HashSet<int>();

        for (int i = 0; i < 1000; i++)
        {
            bool added = idSet.Add(generator.GetNextRequestId());
            Assert.True(added, "Duplicate ID generated.");
        }
    }

    [Fact]
    public void GetNextRequestId_ThreadSafetyTest()
    {
        var generator = new RequestIDGenerator();
        var idSet = new HashSet<int>();
        var tasks = new List<Task>();
        var lockObj = new object();
        bool duplicateFound = false;

        for (int i = 0; i < 10000; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                int id = generator.GetNextRequestId();
                lock (lockObj)
                {
                    if (!idSet.Add(id))
                    {
                        duplicateFound = true;
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.False(duplicateFound, "Duplicate IDs were generated in a multi-threaded environment.");
    }

    [Fact]
    public void GetNextRequestId_RolloverAtMaxValue()
    {
        var generator = new RequestIDGenerator();
        // Setting the field _nextRequestId to int.MaxValue - 1 to test the rollover
        generator.SetNextRequestIdForTesting(int.MaxValue - 1);

        Assert.Equal(int.MaxValue, generator.GetNextRequestId());
        Assert.Equal(1, generator.GetNextRequestId());
    }
}

// Extension for testing purposes (normally, you wouldn't modify your class like this)
public static class RequestIDGeneratorExtensions
{
    public static void SetNextRequestIdForTesting(this RequestIDGenerator generator, int value)
    {
        typeof(RequestIDGenerator)
            .GetField("_nextRequestId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(generator, value);
    }
}
