using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Multithreading_Library.DataTransfer.DeepClone
{
    /// <summary>
    /// Provides functionality to deeply clone objects.
    /// </summary>
    public static class Cloning
    {
        /// <summary>
        /// Creates a deep clone of the specified object.
        /// </summary>
        /// <typeparam name="T">The type of the object being cloned.</typeparam>
        /// <param name="original">The original object to clone.</param>
        /// <returns>A deep clone of the original object.</returns>
        /// <example>
        /// The following code demonstrates how to clone an object deeply:
        /// <code>
        /// MyClass original = new MyClass
        /// {
        ///     Property1 = "Value1",
        ///     Property2 = 3,
        ///     NestedObject = new NestedClass
        ///     {
        ///         NestedProperty = "NestedValue"
        ///     }
        /// };
        /// 
        /// MyClass cloned = original.DeepClone();
        /// </code>
        /// Note: Ensure that all objects to be cloned are serializable or implement IDeepCloneable for custom cloning logic.
        /// </example>
        public static T? DeepClone<T>(this T? original)
        {

            return (T?)new DeepCopyContext().InternalCopy(original, true);
        }
        private class DeepCopyContext
        {
            // ReSharper disable once InconsistentNaming
            private static readonly Func<object, object> _shallowClone;
            // to handle object graphs containing cycles, _visited keeps track of instances we've already cloned
            private readonly Dictionary<object, object> _visited = new(ReferenceEqualityComparer.Instance);

            private readonly Dictionary<Type, FieldInfo[]> _nonShallowFieldCache = new();

            static DeepCopyContext()
            {
                var cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var p1 = Expression.Parameter(typeof(object));
                var body = Expression.Call(p1, cloneMethod);
                _shallowClone = Expression.Lambda<Func<object, object>>(body, p1).Compile();

                // Nullable<T> of deeply immutable value types are themselves deeply immutable
                foreach (var type in ImmutableTypes.Immutables.Where(t => t.IsValueType).ToList())
                {
                    ImmutableTypes.Immutables.Add(typeof(Nullable<>).MakeGenericType(type));
                }
            }

            private static bool IsDeeplyImmutable(Type type)
            {
                
                if (type.IsPrimitive || type.IsEnum) // a little more performant than looking up the hashset
                {
                    return true;
                }
                else
                {
                    return ImmutableTypes.Immutables.Contains(type);
                }
            }

            public object? InternalCopy(object? originalObject, bool includeInObjectGraph)
            {
                if (originalObject == null) return null;

                // immutable types
                var typeToReflect = originalObject.GetType();
                if (IsDeeplyImmutable(typeToReflect) || originalObject is Type) return originalObject;

                // Check if object implements IDeepCloneable and call DeepClone if it does
                if (typeof(IDeepCloneable<>).MakeGenericType(typeToReflect).IsAssignableFrom(typeToReflect))
                {
                    var deepCloneMethod = typeToReflect.GetMethod(nameof(IDeepCloneable<object>.DeepClone));
                    return deepCloneMethod?.Invoke(originalObject, null);
                }

                // 
                if (typeof(XElement).IsAssignableFrom(typeToReflect)) return new XElement((XElement)originalObject);
                if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;


                // reflection from cache
                if (includeInObjectGraph && _visited.TryGetValue(originalObject, out var existingCopy))
                    return existingCopy;

                // reflection based
                var cloneObject = _shallowClone(originalObject);
                if (includeInObjectGraph) _visited.Add(originalObject, cloneObject);

                if (typeToReflect.IsArray) HandleArray(typeToReflect, cloneObject);
                else if (cloneObject is IEnumerable) HandleEnumerable(typeToReflect, cloneObject, includeInObjectGraph);
                else HandleNonCollectionObject(typeToReflect, cloneObject);

                return cloneObject;
            }
            private void HandleArray(Type type, object cloneObject)
            {
                var arrayElementType = type.GetElementType()!;
                if (!IsDeeplyImmutable(arrayElementType))
                {
                    bool isValueType = arrayElementType.IsValueType;
                    EnumerableFill.FillArray((Array)cloneObject, x => InternalCopy(x, !isValueType));
                }
            }
            private void HandleNonCollectionObject(Type type, object cloneObject)
            {
                foreach (var fieldInfo in CachedNonShallowFields(type))
                {
                    var originalFieldValue = fieldInfo.GetValue(cloneObject);
                    var clonedFieldValue = InternalCopy(originalFieldValue, !fieldInfo.FieldType.IsValueType);
                    fieldInfo.SetValue(cloneObject, clonedFieldValue);
                }
            }
            private void HandleEnumerable(Type typeToReflect, object cloneObject, bool includeInObjectGraph)
            {
                if (cloneObject is IList list)
                {
                    if (typeToReflect.IsGenericType && typeToReflect.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var elementType = typeToReflect.GetGenericArguments()[0];
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillList), new[] { elementType }, list, includeInObjectGraph);
                    }
                    else
                    {
                        EnumerableFill.FillArrayList((ArrayList)list, x => InternalCopy(x, includeInObjectGraph));
                    }
                }
                else if (typeToReflect.IsGenericType)
                {
                    var genericTypeDefinition = typeToReflect.GetGenericTypeDefinition();
                    var elementType = typeToReflect.GetGenericArguments()[0];

                    if (genericTypeDefinition == typeof(HashSet<>))
                    {
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillHashSet), new[] { elementType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(Queue<>))
                    {
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillQueue), new[] { elementType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(Stack<>))
                    {
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillStack), new[] { elementType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(Dictionary<,>))
                    {
                        var keyType = typeToReflect.GetGenericArguments()[0];
                        var valueType = typeToReflect.GetGenericArguments()[1];
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillDictionary), new[] { keyType, valueType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(ConcurrentBag<>))
                    {
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillConcurrentBag), new[] { elementType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(ConcurrentQueue<>))
                    {
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillConcurrentQueue), new[] { elementType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(ConcurrentStack<>))
                    {
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillConcurrentStack), new[] { elementType }, cloneObject, includeInObjectGraph);
                    }
                    else if (genericTypeDefinition == typeof(ConcurrentDictionary<,>))
                    {
                        var keyType = typeToReflect.GetGenericArguments()[0];
                        var valueType = typeToReflect.GetGenericArguments()[1];
                        InvokeGenericMethod(typeof(EnumerableFill), nameof(EnumerableFill.FillConcurrentDictionary), new[] { keyType, valueType }, cloneObject, includeInObjectGraph);
                    }
                    // Add additional generic collections as needed
                    else
                    {
                        HandleNonCollectionObject(typeToReflect, cloneObject);
                    }
                }
                else
                {
                    HandleNonCollectionObject(typeToReflect, cloneObject);
                }
            }

            private void InvokeGenericMethod(Type targetType, string methodName, Type[] genericArguments, object targetObject, bool includeInGraph)
            {
                // Ensure the method is searched for correctly, including non-public methods
                var methodInfo = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                if (methodInfo == null)
                {
                    throw new InvalidOperationException($"Method {methodName} not found in type {targetType}.");
                }

                // Make the method generic if necessary
                var genericMethod = methodInfo.MakeGenericMethod(genericArguments);

                // Prepare parameters and invoke the method
                Object[] parameters = new [] { targetObject, new Func<object?, object?>(x => InternalCopy(x, includeInGraph)) };
                genericMethod.Invoke(null, parameters);
            }


            /// <summary>
            /// tries to obtain a field info from cache. If not available, set a new field info
            /// </summary>
            /// <param name="typeToReflect"></param>
            /// <returns></returns>
            private FieldInfo[] CachedNonShallowFields(Type typeToReflect)
            {
                if (!_nonShallowFieldCache.TryGetValue(typeToReflect, out var result))
                {
                    result = NonShallowFields(typeToReflect).ToArray();
                    _nonShallowFieldCache[typeToReflect] = result;
                }
                return result;
            }

            /// <summary>
            /// From the given type hierarchy (i.e. including all base types), return all fields that should be deep-copied
            /// </summary>
            /// <param name="typeToReflect"></param>
            /// <returns></returns>
            private static IEnumerable<FieldInfo> NonShallowFields(Type typeToReflect)
            {
                while (typeToReflect.BaseType != null)
                {
                    foreach (var fieldInfo in typeToReflect.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    {
                        if (IsDeeplyImmutable(fieldInfo.FieldType)) continue; // this is 5% faster than a where clause..
                        yield return fieldInfo;
                    }
                    typeToReflect = typeToReflect.BaseType;
                }
            }
        }
    }
}
