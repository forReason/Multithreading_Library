
using System.Collections;
using System.Collections.Concurrent;

namespace Multithreading_Library.DataTransfer.DeepClone
{
    internal static class EnumerableFill
    {
        internal static void FillMultiDimensionalArray(Array array, Func<object?, object?> func, int dimension, int[] counts, int[] indices)
        {
            int len = counts[dimension];

            if (dimension < (counts.Length - 1))
            {
                // not the final dimension, loop the range, and recursively handle one dimension higher
                for (int t = 0; t < len; t++)
                {
                    indices[dimension] = t;
                    FillMultiDimensionalArray(array, func, dimension + 1, counts, indices);
                }
            }
            else
            {
                // we've reached the final dimension where the elements are closest together in memory. Do a final loop.
                for (int t = 0; t < len; t++)
                {
                    indices[dimension] = t;
                    array.SetValue(func(array.GetValue(indices)), indices);
                }
            }
        }

        internal static void FillArray(Array array, Func<object?, object?> func)
        {
            if (array.Rank == 1)
            {
                // do a fast loop for the common case, a one dimensional array
                int len = array.GetLength(0);
                for (int t = 0; t < len; t++)
                {
                    array.SetValue(func(array.GetValue(t)), t);
                }
            }
            else
            {
                // multidimensional array: recursively loop through all dimensions, starting with dimension zero.
                var counts = Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
                var indices = new int[array.Rank];
                FillMultiDimensionalArray(array, func, 0, counts, indices);
            }
        }
        internal static void FillList<T>(List<T> list, Func<object?, object?> func)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = (T)func(list[i])!;
            }
        }
        internal static void FillArrayList(ArrayList arrayList, Func<object?, object?> func)
        {
            for (int i = 0; i < arrayList.Count; i++)
            {
                arrayList[i] = func(arrayList[i]);
            }
        }
        internal static void FillHashSet<T>(HashSet<T> hashSet, Func<object?, object?> func)
        {
            var items = hashSet.ToList();
            hashSet.Clear();
            foreach (var item in items)
            {
                hashSet.Add((T)func(item)!);
            }
        }
        internal static void FillDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, Func<object?, object?> func) where TKey : notnull
        {
            var keys = dictionary.Keys.ToList();
            foreach (var key in keys)
            {
                dictionary[key] = (TValue)func(dictionary[key])!;
            }
        }

        internal static void FillConcurrentDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue?> dictionary, Func<object?, object?> func) where TKey : notnull
        {
            foreach (var key in dictionary.Keys.ToList())
            {
                if (dictionary.TryGetValue(key, out var value))
                {
                    dictionary[key] = (TValue)func(value)!;
                }
            }
        }

        internal static void FillStack<T>(Stack<T> stack, Func<object?, object?> func)
        {
            var items = stack.ToList();
            stack.Clear();
            foreach (var item in items)
            {
                stack.Push(((T)func(item)!));
            }
        }
        internal static void FillConcurrentStack<T>(ConcurrentStack<T> stack, Func<object?, object?> func)
        {
            var items = new T[stack.Count];
            stack.CopyTo(items, 0);
            stack.Clear();
            foreach (var item in items)
            {
                stack.Push(((T)func(item)!));
            }
        }

        internal static void FillConcurrentBag<T>(ConcurrentBag<T> bag, Func<object?, object?> func)
        {
            var items = new T[bag.Count];
            bag.CopyTo(items, 0);
            bag.Clear();
            foreach (var item in items)
            {
                bag.Add(((T)func(item)!));
            }
        }

        internal static void FillQueue<T>(Queue<T> queue, Func<object?, object?> func)
        {
            var items = queue.ToList();
            queue.Clear();
            foreach (var item in items)
            {
                queue.Enqueue((T)func(item!)!);
            }
        }
        internal static void FillConcurrentQueue<T>(ConcurrentQueue<T> queue, Func<object?, object?> func)
        {
            var items = new T[queue.Count];
            queue.CopyTo(items, 0);
            var newQueue = new ConcurrentQueue<T>();
            foreach (var item in items)
            {
                newQueue.Enqueue((T)func(item!)!);
            }
            Interlocked.Exchange(ref queue, newQueue);
        }

    }
}
