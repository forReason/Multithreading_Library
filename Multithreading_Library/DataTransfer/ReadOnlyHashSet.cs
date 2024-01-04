using System.Collections;

/// <summary>
/// Represents a read-only wrapper around a HashSet.
/// </summary>
/// <typeparam name="T">The type of elements in the hash set.</typeparam>
public class ReadOnlyHashSet<T> : IReadOnlyCollection<T>, IEnumerable<T>
{
    private readonly HashSet<T> _hashSet;

    /// <summary>
    /// Initializes a new instance of the ReadOnlyHashSet class that wraps the specified HashSet.
    /// </summary>
    /// <param name="hashSet">The HashSet to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided HashSet is null.</exception>
    public ReadOnlyHashSet(HashSet<T> hashSet)
    {
        _hashSet = hashSet ?? throw new ArgumentNullException(nameof(hashSet));
    }

    /// <summary>
    /// Initializes a new instance of the ReadOnlyHashSet class that contains the specified Collection elements.
    /// </summary>
    /// <param name="collection">The elements to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided HashSet is null.</exception>
    public ReadOnlyHashSet(IEnumerable<T> collection)
    {
        _hashSet = new HashSet<T>(collection);
    }

    /// <summary>
    /// Gets the number of elements contained in the ReadOnlyHashSet.
    /// </summary>
    public int Count => _hashSet.Count;

    /// <summary>
    /// Determines whether the ReadOnlyHashSet contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the ReadOnlyHashSet.</param>
    /// <returns>true if the ReadOnlyHashSet contains the specified value; otherwise, false.</returns>
    public bool Contains(T item)
    {
        return _hashSet.Contains(item);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the ReadOnlyHashSet.
    /// </summary>
    /// <returns>An enumerator for the ReadOnlyHashSet.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _hashSet.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
