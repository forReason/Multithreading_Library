using System;
using System.Collections.Generic;

namespace Multithreading_Unit_Tests.DataTransfer.OneWrite_MultiRead_Decimal_Tests.Helpers
{
    [Serializable]
    internal class NonCloneableObject
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<string> Clothes { get; set; } = new List<string>();
    }
    [Serializable]
    internal struct NonCloneableStruct
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Clothes { get; set; }
    }
}
