using Multithreading_Library.DataTransfer.DeepClone;
using System;
using System.Collections.Generic;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers
{
    [Serializable]
    internal class CloneableObject : IDeepCloneable<CloneableObject>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Clothes { get; set; } = new List<string>();

        public CloneableObject DeepClone()
        {
            // Create a deep copy of the object
            var clone = new CloneableObject
            {
                Id = this.Id,
                Name = new string(this.Name.ToCharArray()) // Deep copy the string
            };
            foreach (string cloth in Clothes)
            {
                clone.Clothes.Add(new string(cloth.ToCharArray()));
            }
            return clone;
        }
    }
    [Serializable]
    internal struct CloneableStruct : IDeepCloneable<CloneableStruct>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Clothes { get; set; }
        public CloneableStruct DeepClone()
        {
            // Create a deep copy of the object
            var clone = new CloneableStruct
            {
                Id = this.Id,
                Name = new string(this.Name.ToCharArray()),
                Clothes = new List<string>()
            };
            foreach (string cloth in Clothes)
            {
                clone.Clothes.Add(new string(cloth.ToCharArray()));
            }
            return clone;
        }
    }
}
