using Multithreading_Library.DataTransfer;
using Xunit;

namespace Multithreading_Unit_Tests.DataTransfer
{
    public class ConcurrentHashSetTests
    {
        [Fact]
        public void Add_Item_ShouldReturnTrue_WhenNewItem()
        {
            var set = new ConcurrentHashSet<int>();
            bool result = set.Add(1);

            Assert.True(result);
        }

        [Fact]
        public void Add_Item_ShouldReturnFalse_WhenExistingItem()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);
            bool result = set.Add(1);

            Assert.False(result);
        }

        [Fact]
        public void Contains_ShouldReturnTrue_WhenItemExists()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);

            Assert.True(set.Contains(1));
        }

        [Fact]
        public void Contains_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            var set = new ConcurrentHashSet<int>();

            Assert.False(set.Contains(1));
        }

        [Fact]
        public void Remove_ShouldReturnTrue_WhenItemExists()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);

            Assert.True(set.Remove(1));
        }

        [Fact]
        public void Remove_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            var set = new ConcurrentHashSet<int>();

            Assert.False(set.Remove(1));
        }

        [Fact]
        public void Count_ShouldReturnCorrectNumber()
        {
            var set = new ConcurrentHashSet<int> { 1, 2, 3 };

            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void AsHashSet_ShouldReturnHashSetWithSameItems()
        {
            var originalSet = new ConcurrentHashSet<int> { 1, 2, 3 };
            var hashSet = originalSet.AsHashSet();

            Assert.Equal(hashSet, originalSet.AsHashSet());
        }

        [Fact]
        public void AsReadOnlyHashSet_ShouldReturnReadOnlyHashSetWithSameItems()
        {
            var originalSet = new ConcurrentHashSet<int> { 1, 2, 3 };
            var readOnlySet = originalSet.AsReadOnlyHashSet();

            Assert.Equal(readOnlySet, originalSet.AsReadOnlyHashSet());
        }

        [Fact]
        public void Clear_ShouldRemoveAllItems()
        {
            var set = new ConcurrentHashSet<int> { 1, 2, 3 };
            set.Clear();

            Assert.Empty(set.AsHashSet());
        }
    }
}
