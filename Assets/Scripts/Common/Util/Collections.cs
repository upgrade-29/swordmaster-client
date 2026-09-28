using System;
using System.Collections.Generic;
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

    public static bool IsEmpty<T>(this Stack<T> collection)
    {
        return collection.Count == 0;
    }
	
    public static bool IsEmpty<T>(this Queue<T> collection)
    {
        return collection.Count == 0;
    }
	
    public static bool IsEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
	
    public static bool IsEmpty<T>(this T[] collection)
    {
        return collection.Length == 0;
    }
	
    public static bool IsEmpty<T>(this HashSet<T> collection)
    {
        return collection.Count == 0;
    }
	
    public static bool IsEmpty<T>(this List<T> collection)
    {
        return collection.Count == 0;
    }
	
    public static bool IsEmpty<T, Ty>(this Dictionary<T, Ty> collection)
    {
        return collection.Count == 0;
    }
    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
        if(target == null)
            throw new ArgumentNullException(nameof(target));
        if(source == null)
            throw new ArgumentNullException(nameof(source));
        foreach(var element in source)
            target.Add(element);
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