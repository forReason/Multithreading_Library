using System;
using System.Threading;
using Xunit;
using Multithreading_Library.DataTransfer;

namespace Multithreading_Unit_Tests.DataTransfer
{
    public class LazyCacheDictionaryTests
    {
        [Fact]
        public void SetAndGet_ShouldRetrieveCorrectValue()
        {
            var cache = new LazyCacheDictionary<string, int>(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
            cache.Set("key1", 100);

            Assert.Equal(100, cache.Get("key1"));
        }

        [Fact]
        public void Get_ShouldReturnDefaultAfterExpiration()
        {
            var cache = new LazyCacheDictionary<string, int>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(1));
            cache.Set("key1", 100);

            Thread.Sleep(200); // Wait for expiration
            Assert.Equal(0, cache.Get("key1"));
        }

        [Fact]
        public void CleanupTimer_ShouldRemoveExpiredValues()
        {
            var cache = new LazyCacheDictionary<string, int>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
            cache.Set("key1", 100);

            Thread.Sleep(1000); // Wait for cleanup interval
            Assert.Equal(0, cache.Get("key1")); // Value should be removed by cleanup
        }

        [Fact]
        public void Clear_ShouldRemoveAllValues()
        {
            var cache = new LazyCacheDictionary<string, int>(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
            cache.Set("key1", 100);
            cache.Set("key2", 200);

            cache.Clear();
            Assert.Equal(0, cache.Get("key1"));
            Assert.Equal(0, cache.Get("key2"));
        }

        [Fact]
        public void Dispose_ShouldReleaseResources()
        {
            var cache = new LazyCacheDictionary<string, int>(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
            var exception = Record.Exception(() => cache.Dispose());

            Assert.Null(exception);
        }
    }
}
