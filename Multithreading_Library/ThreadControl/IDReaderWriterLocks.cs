using System.Collections.Concurrent;

namespace Multithreading_Library.ThreadControl
{
    /// <summary>
    /// Represents a collection of ReaderWriterLockSlim objects identified by keys of type <typeparamref name="T"/>.
    /// This class allows you to create and manage reader-writer locks on a per-key basis, facilitating
    /// synchronization for read-heavy and write operations in a concurrent environment.
    /// </summary>
    /// <typeparam name="T">The type of the keys used to identify ReaderWriterLockSlim objects.</typeparam>
    public class IDReaderWriterLocks<T> where T : notnull
    {
        private readonly ConcurrentDictionary<T, ReaderWriterLockSlim> _locks = new ConcurrentDictionary<T, ReaderWriterLockSlim>();

        /// <summary>
        /// Obtain (get or create) a ReaderWriterLockSlim object associated with a specific id. If a lock object with the specified
        /// id doesn't already exist, a new one is created. This method is useful for synchronizing access to resources 
        /// that are read-heavy and occasionally written to, and are identified by keys.
        /// </summary>
        /// <param name="key">The key to identify the ReaderWriterLockSlim object.</param>
        /// <returns>A ReaderWriterLockSlim object that can be used for reader-writer synchronization.</returns>
        public ReaderWriterLockSlim ObtainLockObject(T key)
        {
            return _locks.GetOrAdd(key, _ => new ReaderWriterLockSlim());
        }
    }
}
