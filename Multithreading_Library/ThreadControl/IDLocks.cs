using System.Collections.Concurrent;

namespace Multithreading_Library.ThreadControl
{
    public class IDLocks<T>
    {
        ConcurrentDictionary<T,object> locks = new ConcurrentDictionary<T,object>();
        /// <summary>
        /// obtain (get or create) a lock object with the specific id)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object ObtainLockObject(T key)
        {
            return locks.GetOrAdd(key, new object());
        }
    }
}
