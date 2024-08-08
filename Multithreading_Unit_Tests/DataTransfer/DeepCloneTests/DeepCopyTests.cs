using Multithreading_Library.DataTransfer.DeepClone;
using Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers;
using Xunit;

namespace Multithreading_Unit_Tests.DataTransfer.DeepCloneTests
{
#pragma warning disable xUnit2002
#pragma warning disable xUnit2005
    public class DeepCopyTests
    {
        [Fact]
        public void Test_DeepClone_Decimal()
        {
            decimal input = 10000000000;
            decimal output = Cloning.DeepClone(input);
            Assert.Equal(input, output);
        }
        [Fact]
        public void Test_DeepClone_CloneableStruct()
        {
            // Arrange
            var original = new CloneableStruct()
            {
                Id = 1,
                Name = "Original",
                Clothes = new() { "Pant", "Socks" }
            };

            // Act
            var cloned = Cloning.DeepClone(original);

            // Assert

            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned); // Ensure it's a different instance
            Assert.Equal(original.Id, cloned.Id);
            Assert.Equal(original.Name, cloned.Name);


            // change values to make sure we are not destroying things
            cloned.Name = "Clone";
            cloned.Id = 2;
            cloned.Clothes.AddRange(new[] { "Shirt", "Glasses" } );

            // Ensure that the string is deeply copied
            Assert.NotSame(original.Name, cloned.Name);
            Assert.NotEqual(original.Id, cloned.Id);
            Assert.NotEqual(original.Name, cloned.Name);
            Assert.NotEqual(original.Clothes.Count, cloned.Clothes.Count);
        }
        [Fact]
        public void DeepClone_NonCloneableStruct()
        {
            // Arrange
            var original = new NonCloneableStruct()
            {
                Id = 1,
                Name = "Original",
                Clothes = new() { "Pant", "Socks" }
            };

            // Act
            var cloned = Cloning.DeepClone(original);

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned); // Ensure it's a different instance
            Assert.Equal(original.Id, cloned.Id);
            Assert.Equal(original.Name, cloned.Name);

            // change values to make sure we are not destroying things
            cloned.Name = "Clone";
            cloned.Id = 2;
            cloned.Clothes.AddRange(new[] { "Shirt", "Glasses" });

            // Ensure that the string is deeply copied
            Assert.NotSame(original.Name, cloned.Name);
            Assert.NotEqual(original.Id, cloned.Id);
            Assert.NotEqual(original.Name, cloned.Name);
            Assert.NotEqual(original.Clothes.Count, cloned.Clothes.Count);
        }
        [Fact]
        public void DeepClone_CloneableType()
        {
            // Arrange
            var original = new CloneableObject()
            {
                Id = 1,
                Name = "Original",
                Clothes = new() { "Pant", "Socks" }
            };

            // Act
            var cloned = Cloning.DeepClone(original);

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned); // Ensure it's a different instance
            Assert.Equal(original.Id, cloned!.Id);
            Assert.Equal(original.Name, cloned.Name);


            // change values to make sure we are not destroying things
            cloned.Name = "Clone";
            cloned.Id = 2;
            cloned.Clothes.AddRange(new[] { "Shirt", "Glasses" });

            // Ensure that the string is deeply copied
            Assert.NotSame(original.Name, cloned.Name);
            Assert.NotEqual(original.Id, cloned.Id);
            Assert.NotEqual(original.Name, cloned.Name);
            Assert.NotEqual(original.Clothes.Count, cloned.Clothes.Count);
        }
        [Fact]
        public void DeepClone_NonCloneableType()
        {
            // Arrange
            var original = new NonCloneableObject()
            {
                Id = 1,
                Name = "Original",
                Clothes =  new() {"Pant", "Socks" }
            };

            // Act
            var cloned = Cloning.DeepClone(original);

            // Assert
            Assert.NotNull(cloned);
            Assert.NotSame(original, cloned); // Ensure it's a different instance
            Assert.Equal(original.Id, cloned!.Id);
            Assert.Equal(original.Name, cloned.Name);

            // change values to make sure we are not destroying things
            cloned.Name = "Clone";
            cloned.Id = 2;
            cloned.Clothes.AddRange(new[] { "Shirt", "Glasses" });

            // Ensure that the string is deeply copied
            Assert.NotSame(original.Name, cloned.Name);
            Assert.NotEqual(original.Id, cloned.Id);
            Assert.NotEqual(original.Name, cloned.Name);
            Assert.NotEqual(original.Clothes.Count, cloned.Clothes.Count);
        }
    }
#pragma warning restore xUnit2002
#pragma warning restore xUnit2005
}
