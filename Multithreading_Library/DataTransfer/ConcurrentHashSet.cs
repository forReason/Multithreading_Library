using System.Collections;
using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer
{
    /// <summary>
    /// Represents a thread-safe set of values.
    /// </summary>
    /// <typeparam name="T">The type of elements in the hash set.</typeparam>
    public class ConcurrentHashSet<T> : IReadOnlyCollection<T> where T : notnull
    {
        /// <summary>
        /// Initializes a new instance of the ConcurrentHashSet class that is empty.
        /// </summary>
        public ConcurrentHashSet()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentHashSet class that contains elements copied from the specified collection.
        /// </summary>
        /// <param name="elements">The collection whose elements are copied to the new set.</param>
        public ConcurrentHashSet(IEnumerable<T> elements)
        {
            foreach (var element in elements)
            {
                Add(element);
            }
        }

        private readonly ConcurrentDictionary<T, T> _dictionary = new();

        /// <summary>
        /// Tries to add an element to the ConcurrentHashSet.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <returns>true if the element is added successfully; false if the element already exists.</returns>
        public bool Add(T item)
        {
            return _dictionary.TryAdd(item, item);
        }

        /// <summary>
        /// Determines whether the ConcurrentHashSet contains a specific value.
        /// </summary>
        /// <param name="item">The value to locate in the ConcurrentHashSet.</param>
        /// <returns>true if the ConcurrentHashSet contains the specified value; otherwise, false.</returns>
        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        /// <summary>
        /// Removes the specified element from the ConcurrentHashSet.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <param name="removedItem">the item which was removed</param>
        /// <returns>true if the element was successfully found and removed; otherwise, false.</returns>
        public bool TryRemove(T item, out T? removedItem)
        {
            return _dictionary.TryRemove(item, out removedItem);
        }

        /// <summary>
        /// Adds an element to the ConcurrentHashSet if it does not already exist, or replaces an existing element.
        /// </summary>
        /// <param name="item">The element to add or update.</param>
        /// <remarks>
        /// This is specifically relevant for structs and classes where a custom .Equals and .GetHashCode are defined
        /// </remarks>
        public void AddOrReplace(T item)
        {
            _dictionary.AddOrUpdate(item, item, (key, oldValue) => item);
        }

        /// <summary>
        /// Removes all elements from the ConcurrentHashSet.
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        /// <summary>
        /// Gets the number of elements contained in the ConcurrentHashSet.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Creates a new HashSet containing all the elements in the ConcurrentHashSet.
        /// </summary>
        /// <returns>A new HashSet containing all the elements.</returns>
        /// <remarks>
        /// This method creates a snapshot of the current state of the ConcurrentHashSet. Changes to the ConcurrentHashSet
        /// after this method is called will not be reflected in the returned HashSet.
        /// </remarks>
        public HashSet<T> AsHashSet()
        {
            return [.._dictionary.Values];
        }

        /// <summary>
        /// Creates a new ReadOnlyHashSet containing all the elements in the ConcurrentHashSet.
        /// </summary>
        /// <returns>A new ReadOnlyHashSet containing all the elements.</returns>
        /// <remarks>
        /// This method creates a snapshot of the current state of the ConcurrentHashSet. Changes to the ConcurrentHashSet
        /// after this method is called will not be reflected in the returned ReadOnlyHashSet.
        /// </remarks>
        public ReadOnlyHashSet<T> AsReadOnlyHashSet()
        {
            return new ReadOnlyHashSet<T>(_dictionary.Values);
        }
        /// <summary>
        /// Returns the enumerator for the Hashset values
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
