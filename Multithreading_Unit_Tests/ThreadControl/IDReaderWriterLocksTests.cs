using Multithreading_Library.ThreadControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Multithreading_Unit_Tests.ThreadControl
{
    public class IDReaderWriterLocksTests
    {
        [Fact]
        public void ObtainLockObject_WithKey_ReturnsReaderWriterLockSlim()
        {
            // Arrange
            var idReaderWriterLocks = new IDReaderWriterLocks<string>();

            // Act
            var lockSlim = idReaderWriterLocks.ObtainLockObject("testKey");

            // Assert
            Assert.NotNull(lockSlim);
            Assert.IsType<ReaderWriterLockSlim>(lockSlim);
        }

        [Fact]
        public void ObtainLockObject_WithSameKey_ReturnsSameLockInstance()
        {
            // Arrange
            var idReaderWriterLocks = new IDReaderWriterLocks<string>();
            var firstLock = idReaderWriterLocks.ObtainLockObject("testKey");

            // Act
            var secondLock = idReaderWriterLocks.ObtainLockObject("testKey");

            // Assert
            Assert.Same(firstLock, secondLock);
        }

        [Fact]
        public void ObtainLockObject_WithDifferentKeys_ReturnsDifferentLockInstances()
        {
            // Arrange
            var idReaderWriterLocks = new IDReaderWriterLocks<string>();
            var firstLock = idReaderWriterLocks.ObtainLockObject("testKey1");
            var secondLock = idReaderWriterLocks.ObtainLockObject("testKey2");

            // Act and Assert
            Assert.NotSame(firstLock, secondLock);
        }
    }
}