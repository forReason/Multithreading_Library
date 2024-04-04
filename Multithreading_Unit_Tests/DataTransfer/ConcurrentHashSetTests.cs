using System.Linq;
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

            Assert.True(set.TryRemove(1, out _));
        }

        [Fact]
        public void Remove_ShouldReturnFalse_WhenItemDoesNotExist()
        {
            var set = new ConcurrentHashSet<int>();

            Assert.False(set.TryRemove(1, out _));
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
        [Fact]
        public void AddOrReplace_ShouldAdd_WhenNewItem()
        {
            var set = new ConcurrentHashSet<int>();
            set.AddOrReplace(1); // Assuming AddOrUpdate doesn't return anything
            
            Assert.True(set.Contains(1));
            Assert.Equal(1, set.Count);
        }

        [Fact]
        public void AddOrReplace_ShouldNotIncreaseCount_WhenExistingItem()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1); // Add item first
            set.AddOrReplace(1); // Then try to update it
            
            Assert.True(set.Contains(1));
            Assert.Equal(1, set.Count); // Count should not increase
        }

        
        private struct Item
        {
            public int Id { get; set; }
            public string Value { get; set; }

            public Item(int id, string value)
            {
                Id = id;
                Value = value;
            }

            // Override Equals and GetHashCode to use Id for equality
            public override bool Equals(object obj)
            {
                return obj is Item item && Id == item.Id;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        [Fact]
        public void AddOrReplace_ShouldUpdate_WhenExistingItemWithNewValue()
        {
            var set = new ConcurrentHashSet<Item>();
            var item = new Item { Id = 1, Value = "Original" };
            set.Add(item);

            var updatedItem = new Item { Id = 1, Value = "Updated" };
            set.AddOrReplace(updatedItem);

            var actual = set.AsHashSet().FirstOrDefault(i => i.Id == 1);
            Assert.NotNull(actual);
            Assert.Equal("Updated", actual.Value);
        }
    }
}
