using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multithreading_Library.ThreadControl
{
    public class IDLocks<T>
    {
        ConcurrentDictionary<T,object> locks = new ConcurrentDictionary<T,object>();
        public object ObtainLockObject(T key)
        {
            return locks.GetOrAdd(key, new object());
        }
    }
}
