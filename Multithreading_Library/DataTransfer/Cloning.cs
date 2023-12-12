
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Multithreading_Library.DataTransfer
{
    public static class CloningOld
    {
        /// <summary>
        /// this function creates a DeepClone of an object.<BR/>
        /// It supports the following types without resorting to Reflection:<BR/>
        /// - Value Types<BR/>
        /// - IEnumerable (containing ICloneable types)
        /// - Custom types and type which implement the interface ICloneable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <returns></returns>
        public static T DeepClone<T>(T original)
        {
            // Check for null
            if (original == null)
            {
                return default;
            }

            // Handle value types
            if (typeof(T).IsValueType)
            {
                // If it's a struct, we need to clone each field
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal))
                {
                    return original; // Primitives, strings, and decimals are immutable
                }
            }

            // Handle Arrays
            if (typeof(T).IsArray)
            {
                var array = original as Array;
                var clonedArray = Array.CreateInstance(typeof(T).GetElementType(), array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    clonedArray.SetValue(DeepClone((T)array.GetValue(i)), i);
                }
                return (T)Convert.ChangeType(clonedArray, typeof(T));
            }


            // Handle IEnumerable types
            if (original is IEnumerable<T> enumerable)
            {
                Type type = original.GetType();

                if (type.IsGenericType)
                {
                    Type genericTypeDefinition = type.GetGenericTypeDefinition();
                    Type[] genericArguments = type.GetGenericArguments();

                    // Handle List<T>
                    if (genericTypeDefinition == typeof(List<>))
                    {
                        var clonedList = (IList<T>)Activator.CreateInstance(type);
                        foreach (var item in enumerable)
                        {
                            clonedList.Add(DeepClone(item));
                        }
                        return (T)clonedList;
                    }

                    // Handle ArrayList
                    if (genericTypeDefinition == typeof(ArrayList))
                    {
                        var clonedArrayList = new ArrayList();
                        foreach (var item in enumerable)
                        {
                            clonedArrayList.Add(DeepClone(item));
                        }
                        return (T)(object)clonedArrayList;
                    }

                    // Handle HashSet<T>
                    if (genericTypeDefinition == typeof(HashSet<>))
                    {
                        dynamic clonedHashSet = Activator.CreateInstance(type);
                        foreach (var item in enumerable)
                        {
                            clonedHashSet.Add(DeepClone(item));
                        }
                        return (T)clonedHashSet;
                    }
                    // Handle Queue<T> and ConcurrentQueue<T>
                    if (genericTypeDefinition == typeof(Queue<>) || genericTypeDefinition == typeof(ConcurrentQueue<>))
                    {
                        dynamic clonedQueue = Activator.CreateInstance(type);
                        foreach (var item in enumerable)
                        {
                            clonedQueue.Enqueue(DeepClone(item));
                        }
                        return (T)clonedQueue;
                    }

                    // Handle ConcurrentBag (assuming ConcurrentBag is a custom collection)
                    if (genericTypeDefinition == typeof(ConcurrentBag<>))
                    {
                        dynamic clonedConcurrentBag = Activator.CreateInstance(type);
                        foreach (var item in enumerable)
                        {
                            clonedConcurrentBag.Add(DeepClone(item));
                        }
                        return (T)clonedConcurrentBag;
                    }

                    // Handle Stack
                    if (genericTypeDefinition == typeof(Stack<>))
                    {
                        var clonedStack = new Stack<T>();
                        var tempStack = new Stack<T>(enumerable);
                        var tempArray = tempStack.ToArray();
                        for (int i = tempArray.Length - 1; i >= 0; i--)
                        {
                            clonedStack.Push(DeepClone(tempArray[i]));
                        }
                        return (T)(object)clonedStack;
                    }

                    // Handle ConcurrentStack (assuming ConcurrentStack is a custom collection)
                    if (genericTypeDefinition == typeof(ConcurrentStack<>))
                    {
                        dynamic clonedConcurrentStack = Activator.CreateInstance(type);
                        foreach (var item in enumerable)
                        {
                            clonedConcurrentStack.Push(DeepClone(item));
                        }
                        return (T)clonedConcurrentStack;
                    }

                    // Handle Dictionary<TKey, TValue>
                    if (genericTypeDefinition == typeof(Dictionary<,>))
                    {
                        var clonedDictionary = (IDictionary)Activator.CreateInstance(type);
                        var dictionaryType = typeof(T);
                        var keyProperty = dictionaryType.GetProperty("Key");
                        var valueProperty = dictionaryType.GetProperty("Value");

                        foreach (var entry in enumerable)
                        {
                            var key = DeepClone(keyProperty.GetValue(entry));
                            var value = DeepClone(valueProperty.GetValue(entry));
                            clonedDictionary.Add(key, value);
                        }
                        return (T)(object)clonedDictionary;
                    }

                    // Handle ConcurrentDictionary<TKey, TValue> (assuming ConcurrentDictionary is a custom collection)
                    if (genericTypeDefinition == typeof(ConcurrentDictionary<,>))
                    {
                        dynamic clonedConcurrentDictionary = Activator.CreateInstance(type);
                        var dictionaryType = typeof(T);
                        var keyProperty = dictionaryType.GetProperty("Key");
                        var valueProperty = dictionaryType.GetProperty("Value");

                        foreach (var entry in enumerable)
                        {
                            var key = DeepClone(keyProperty.GetValue(entry));
                            var value = DeepClone(valueProperty.GetValue(entry));
                            clonedConcurrentDictionary.TryAdd(key, value);
                        }
                        return (T)clonedConcurrentDictionary;
                    }


                    // Add cases for other generic collections like HashSet<T>, Dictionary<TKey, TValue>, etc.

                }
                // Add cases for non-generic collections if needed.
            }
            // Handle ICloneable types
            else if (original is ICloneable cloneable)
            {
                return (T)cloneable.Clone();
            }

            // Fallback for other reference types
            return ReflectionBasedClone(original);
        }

        private static T ReflectionBasedClone<T>(T original)
        {
            if (original == null)
            {
                return default(T);
            }

            Type type = typeof(T);

            // Handle immutable types and value types
            if (type.IsValueType || type == typeof(string))
            {
                return original;
            }

            // Create a new instance of the object
            T clonedObject = (T)Activator.CreateInstance(type);

            // Clone fields
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object fieldValue = field.GetValue(original);
                field.SetValue(clonedObject, CloneElement(fieldValue));
            }

            // Clone properties
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.CanWrite)
                {
                    object propertyValue = property.GetValue(original);
                    property.SetValue(clonedObject, CloneElement(propertyValue));
                }
            }

            return clonedObject;
        }





        private static object CloneElement(object element)
        {
            // Recursively clone elements
            if (element == null)
            {
                return null;
            }

            Type elementType = element.GetType();
            if (elementType.IsValueType || elementType == typeof(string))
            {
                return element; // Value types and strings are immutable, so just return the element.
            }

            if (element is ICloneable cloneable)
            {
                return cloneable.Clone();
            }

            return ReflectionBasedClone(element); // Fallback to reflection-based cloning for non-ICloneable types
        }
    }
}
