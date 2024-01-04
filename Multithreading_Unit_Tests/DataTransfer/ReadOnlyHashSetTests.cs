using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Multithreading_Unit_Tests.DataTransfer
{
    public class ReadOnlyHashSetTests
    {
        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenHashSetIsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => new ReadOnlyHashSet<int>(null as HashSet<int>));
        }

        [Fact]
        public void Constructor_ShouldInitialize_WhenHashSetIsNotNull()
        {
            var hashSet = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlyHashSet<int>(hashSet);

            Assert.Equal(hashSet.Count, readOnlySet.Count);
        }

        [Fact]
        public void Constructor_ShouldInitialize_WhenCollectionIsNotNull()
        {
            var collection = new List<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlyHashSet<int>(collection);

            Assert.Equal(collection.Count, readOnlySet.Count);
        }

        [Fact]
        public void Contains_ShouldReturnTrue_WhenItemExists()
        {
            var hashSet = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlyHashSet<int>(hashSet);

            Assert.True(readOnlySet.Contains(1));
        }

        [Fact]
        public void Contains_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            var hashSet = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlyHashSet<int>(hashSet);

            Assert.False(readOnlySet.Contains(4));
        }

        [Fact]
        public void GetEnumerator_ShouldEnumerateAllItems()
        {
            var hashSet = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlyHashSet<int>(hashSet);
            var enumeratedItems = readOnlySet.ToList();

            Assert.Equal(hashSet.Count, enumeratedItems.Count);
            foreach (var item in hashSet)
            {
                Assert.Contains(item, enumeratedItems);
            }
        }

        [Fact]
        public void Count_ShouldReturnCorrectNumberOfItems()
        {
            var hashSet = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlyHashSet<int>(hashSet);

            Assert.Equal(3, readOnlySet.Count);
        }
    }
}
