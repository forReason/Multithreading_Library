using System.Collections.Concurrent;

namespace Multithreading_Library.ThreadControl
{
    /// <summary>
    /// Represents a collection of SemaphoreSlim objects identified by keys of type <typeparamref name="T"/>.
    /// This class allows you to create and manage semaphores on a per-key basis, facilitating
    /// synchronization and resource control in a concurrent environment.
    /// </summary>
    /// <typeparam name="T">The type of the keys used to identify SemaphoreSlim objects.</typeparam>
    public class IDSemaphoreSlim<T>
    {
        ConcurrentDictionary<T,SemaphoreSlim> locks = new ConcurrentDictionary<T, SemaphoreSlim>();
        /// <summary>
        /// Obtain (get or create) a lock object with the specified id. If the lock object doesn't exist,
        /// it's created with an initial count of 1 and a maximum count of 1. This method is typically used
        /// when you want to start with one available entry in the semaphore, and you want to ensure that
        /// no more than one thread can enter the semaphore at any time.
        /// </summary>
        /// <param name="key">The key to identify the lock object.</param>
        /// <returns>A SemaphoreSlim object associated with the specified key.</returns>
        public SemaphoreSlim ObtainLockObject(T key)
        {
            return locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Obtain (get or create) a lock object with the specified id and a custom initial count.
        /// The maximum count for the semaphore is set to int.MaxValue. This overload is useful when
        /// you want to allow more than one thread to enter the semaphore concurrently right from the start,
        /// without setting a specific upper limit on the count.
        /// </summary>
        /// <param name="key">The key to identify the lock object.</param>
        /// <param name="initialCount">The initial count for the semaphore, representing the number of
        /// threads that can enter the semaphore without waiting.</param>
        /// <returns>A SemaphoreSlim object associated with the specified key.</returns>
        public SemaphoreSlim ObtainLockObject(T key, int initialCount)
        {
            return locks.GetOrAdd(key, _ => new SemaphoreSlim(initialCount));
        }

        /// <summary>
        /// Obtain (get or create) a lock object with the specified id, custom initial count, and custom
        /// maximum count. This overload allows for full control over the initial and maximum number of
        /// threads that can enter the semaphore. The initial count specifies how many threads can initially
        /// enter the semaphore without being blocked, while the maximum count sets an upper limit on the
        /// number of concurrent entries allowed in the semaphore.
        /// </summary>
        /// <param name="key">The key to identify the lock object.</param>
        /// <param name="initialCount">The initial count for the semaphore.</param>
        /// <param name="maxCount">The maximum count for the semaphore, representing the absolute limit
        /// on the number of concurrent entries allowed.</param>
        /// <returns>A SemaphoreSlim object associated with the specified key.</returns>
        public SemaphoreSlim ObtainLockObject(T key, int initialCount, int maxCount)
        {
            return locks.GetOrAdd(key, _ => new SemaphoreSlim(initialCount, maxCount));
        }
    }
}
