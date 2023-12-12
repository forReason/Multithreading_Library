using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Multithreading_Library.DataTransfer.DeepClone
{
    internal class ImmutableTypes
    {
        /// <summary>
        /// immutable types which are safe to copy
        /// </summary>
        /// <remarks>
        /// Nullable types which are derivations of these are beeing handled automatically
        /// </remarks>
        internal static readonly HashSet<Type> Immutables = new()
        {
            typeof(nint),
            typeof(nuint),
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(Half),
            typeof(Complex),
            typeof(BigInteger),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateOnly),
            typeof(TimeOnly),
            typeof(TimeSpan),
            typeof(DateTimeOffset),
            typeof(Range),
            typeof(Index),
            typeof(DBNull),
            typeof(Version),
            typeof(Uri),
        };
    }
}
