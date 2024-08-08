using System.Collections.Concurrent;

namespace Multithreading_Library.ThreadControl
{
    /// <summary>
    /// Represents a collection of lock objects identified by keys of type <typeparamref name="T"/>.
    /// This class facilitates the creation and management of lock objects for synchronization purposes
    /// in a concurrent environment.
    /// </summary>
    /// <typeparam name="T">The type of the keys used to identify lock objects.</typeparam>
    public class IDLocks<T> where T : notnull
    {
        private readonly ConcurrentDictionary<T, object> _locks = new ();

        /// <summary>
        /// Obtain (get or create) a lock object associated with a specific id. If a lock object with the specified
        /// id doesn't already exist, a new one is created. This method is useful for synchronizing access
        /// to resources that are identified by keys.
        /// </summary>
        /// <param name="key">The key to identify the lock object.</param>
        /// <returns>An object that can be used as a lock in synchronization scenarios.</returns>
        public object ObtainLockObject(T key)
        {
            return _locks.GetOrAdd(key, _ => new object());
        }
    }

}
