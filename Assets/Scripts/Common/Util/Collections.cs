using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;

public static class Collections
{
    [CanBeNull] 
    public static TValue? ElementAtOrNull<TValue>(this IReadOnlyList<TValue> list, int index)
        where TValue : struct
    {
        if (index < 0 || index >= list.Count)
        {
            return null;
        }

        return list.ElementAt(index);
    }

    [CanBeNull]
    public static TValue? GetValueOrNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : struct
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;
        
        return null;
    }

    [CanBeNull]
    public static TValue GetOrNull<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key
    ) where TValue : class
    {
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }

    public static TValue GetOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default
    )
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public static bool AddOnlyNotNull<TValue>(this ICollection<TValue> coll, TValue elem)
    {
        if (elem == null)
            return false;

        coll.Add(elem);
        return true;
    }

    public static bool AddOnlyNotZero(this ICollection<int> coll, int elem)
    {
        if (elem == 0)
            return false;

        coll.Add(elem);
        return true;
    }

    public static bool AddOnlyNotZero(this ICollection<long> coll, long elem)
    {
        if (elem == 0L)
            return false;

        coll.Add(elem);
        return true;
    }

    public static bool AddOnlyNotNull<TKey, TValue>(this SortedDictionary<TKey, TValue> coll, TKey key,
        TValue? elem) where TValue : struct
    {
        if (key == null || elem == null)
            return false;

        coll.Add(key, elem.Value);
        return true;
    }

    public static bool AddOnlyNotNull<TKey, TValue>(this SortedDictionary<TKey, TValue> coll, TKey key, TValue elem)
        where TValue : class
    {
        if (key == null || elem == null)
            return false;

        coll.Add(key, elem);
        return true;
    }

    public static int AddRangeOnlyNotNull<T>(this List<T> list, [CanBeNull] IEnumerable<T> elems)
    {
        if (elems == null)
            return 0;

        var filtered = elems.Where(model => model != null).ToList();
        list.AddRange(filtered);
        return filtered.Count;
    }

    public static List<T> CloneToWritableList<T>(this IEnumerable<T> list)
    {
        if (list == null)
            return new List<T>();
        return new List<T>(list);
    }

    public static void RemoveAll<T>(this List<T> list, IEnumerable<T> toRemove)
    {
        if (list == null || toRemove == null)
            return;
        list.RemoveAll(toRemove.Contains);
    }
    
    [NotNull]
    public static IReadOnlyList<IReadOnlyList<T>> SplitByChunk<T>(this IEnumerable<T> list, int chunkSize)
    {
        var result = list
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList().AsReadOnly())
            .ToList().AsReadOnly();
        return result;
    }

    public static List<T> Combine<T>(params IEnumerable<T>[] lists)
    {
        if (lists == null)
            return new List<T>();
        var combined = new List<T>();
        foreach (var sublist in lists)
        {
            if (sublist == null)
                continue;
            combined.AddRange(sublist);
        }

        return combined;
    }

    [NotNull]
    public static NameValueCollection ToNameValueCollectionByValueToString<TValue>(
        [NotNull] IDictionary<string, TValue> map)
    {
        var result = ToNameValueCollection(map.ToDictionary(e => e.Key, e => e.Value.ToString()));
        return result;
    }

    [NotNull]
    public static NameValueCollection ToNameValueCollection([NotNull] IDictionary<string, string> map)
    {
        var result = new NameValueCollection();
        foreach (var kvp in map)
        {
            result.Add(kvp.Key, kvp.Value);
        }
        return result;
    }

    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T>([CanBeNull] this IEnumerable<T> list)
    {
        bool result = list == null || !list.Any();
        return result;
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T>([CanBeNull] this Stack<T> list)
    {
        bool result = list == null || list.Count == 0;
        return result;
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T>([CanBeNull] this Queue<T> list)
    {
        bool result = list == null || list.Count == 0;
        return result;
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty([CanBeNull] this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T>([CanBeNull] this T[] collection)
    {
        bool result = collection == null || collection.Length == 0;
        return result;
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T>([CanBeNull] this HashSet<T> list)
    {
        bool result = list == null || list.Count == 0;
        return result;
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T>([CanBeNull] this List<T> list)
    {
        bool result = list == null || list.Count == 0;
        return result;
    }
    
    [ContractAnnotation("null => true")]
    public static bool IsNullOrEmpty<T, Ty>([CanBeNull] this Dictionary<T, Ty> collection)
    {
        bool result = collection == null || collection.Count == 0;
        return result;
    }
    
    public static void RemoveAt<T>(ref T[] array, int index)
    {
        var newLength = array.Length - 1;
        for (int i = index; i < newLength; i++)
        {
            array[i] = array[i + 1];
        }
        
        var destinationArray = new T[newLength];
        for (int i = 0; i < newLength; i++)
        {
            destinationArray[i] = array[i];
        }

        array = destinationArray;
    }
}