using System;
using System.Threading;
using Xunit;
using Multithreading_Library.DataTransfer;

namespace Multithreading_Unit_Tests.DataTransfer
{
    public class LazyCacheTests
    {
        [Fact]
        public void Value_ShouldBeRetrievable_AfterSetting()
        {
            var cache = new LazyCache<int>(TimeSpan.FromSeconds(5));
            cache.Value = 42;
            Assert.Equal(42, cache.Value);
        }

        [Fact]
        public void Value_ShouldBeDefault_AfterExpiration()
        {
            var cache = new LazyCache<int>(TimeSpan.FromMilliseconds(100));
            cache.Value = 42;
            Thread.Sleep(200); // Wait for expiration
            Assert.Equal(default, cache.Value);
        }

        [Fact]
        public void Value_ShouldBeDefault_AfterClearing()
        {
            var cache = new LazyCache<int>(TimeSpan.FromSeconds(5));
            cache.Value = 42;
            cache.Clear();
            Assert.Equal(default, cache.Value);
        }

        [Fact]
        public void Cache_ShouldDisposeWithoutError()
        {
            var cache = new LazyCache<int>(TimeSpan.FromSeconds(5));
            var exception = Record.Exception(() => cache.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Cache_ShouldBeThreadSafe()
        {
            var cache = new LazyCache<int>(TimeSpan.FromSeconds(5));
            int value1 = 0, value2 = 0;

            var thread1 = new Thread(() => { cache.Value = 42; });
            var thread2 = new Thread(() => { value1 = cache.Value; });

            thread1.Start();
            thread1.Join();

            thread2.Start();
            thread2.Join();

            var thread3 = new Thread(() => { value2 = cache.Value; });
            thread3.Start();
            thread3.Join();

            Assert.Equal(42, value1);
            Assert.Equal(42, value2);
        }
    }
}
