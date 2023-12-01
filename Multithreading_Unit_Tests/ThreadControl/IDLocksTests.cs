using Xunit;
using Multithreading_Library.ThreadControl;
using System.Collections.Generic;
using System.Threading.Tasks;

public class IDLocksTests
{
    [Fact]
    public void ObtainLockObject_ReturnsSameObjectForSameKey()
    {
        var idLocks = new IDLocks<int>();
        var key = 123;

        var lockObject1 = idLocks.ObtainLockObject(key);
        var lockObject2 = idLocks.ObtainLockObject(key);

        Assert.Same(lockObject1, lockObject2);
    }

    [Fact]
    public void ObtainLockObject_ReturnsDifferentObjectsForDifferentKeys()
    {
        var idLocks = new IDLocks<int>();
        var key1 = 123;
        var key2 = 456;

        var lockObject1 = idLocks.ObtainLockObject(key1);
        var lockObject2 = idLocks.ObtainLockObject(key2);

        Assert.NotSame(lockObject1, lockObject2);
    }

    [Fact]
    public void ObtainLockObject_ThreadSafetyTest()
    {
        var idLocks = new IDLocks<int>();
        var key = 123;
        var lockObjects = new HashSet<object>();
        var tasks = new List<Task>();
        var lockObj = new object();
        bool sameObjectAlwaysReturned = true;

        for (int i = 0; i < 10000; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var obtainedLock = idLocks.ObtainLockObject(key);
                lock (lockObj)
                {
                    if (!lockObjects.Contains(obtainedLock))
                    {
                        lockObjects.Add(obtainedLock);
                    }

                    if (lockObjects.Count > 1)
                    {
                        sameObjectAlwaysReturned = false;
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.True(sameObjectAlwaysReturned, "Different lock objects were returned for the same key in a multi-threaded environment.");
    }
}
