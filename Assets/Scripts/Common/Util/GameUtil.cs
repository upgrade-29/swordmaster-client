using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public static class GameUtil
{
	public static T Bind<T>(GameObject parent, ref T target) where T: Component
	{
		if (target != null)
		{
			return target;
		}
		
		target = TryGetComponent<T>(parent);
		return target;
	}
	
	public static void BindOrAdd<T>(GameObject parent, ref T target) where T: Component
	{
		if (target != null)
		{
			return;
		}
		
		target = TryGetOrAddComponent<T>(parent);
	}
	
	public static T TryGetComponent<T>(GameObject parent) where T: Component
	{
		Assert.IsNotNull(parent);
		
		var target = parent.GetComponent<T>();
		if (target != null)
		{
			return target;
		}
		
		Debug.LogError($"Failed to find a component [{typeof(T).Name}] [{parent}]");
		return null;
	}
	
	public static T TryGetOrAddComponent<T>(GameObject parent) where T: Component
	{
		Assert.IsNotNull(parent);
		
		var target = parent.GetComponent<T>();
		if (target != null)
		{
			return target;
		}
		
		target = parent.AddComponent<T>();
		Assert.IsNotNull(target);
		if (target == null)
		{
			Debug.LogError("Why added component is null?");
		}
		return target;
	}
	
	public static T Bind<T>(Transform parent, string path) where T: Component
	{
		Assert.IsNotNull(parent);
		Assert.IsFalse(string.IsNullOrEmpty(path));
		
		Transform child = parent.transform.Find(path);
		if (child == null)
		{
			Debug.LogError($"bind target null, path:{parent.gameObject.GetScenePath()}");
			throw new Exception($"Failed to find a gameObject in path({path})");
		}
		
		if (child.TryGetComponent<T>(out var comp) == false)
		{
			throw new Exception($"Failed to find a component({typeof(T).Name}) from GameObject({path})");
		}
		
		return comp;
	}
	
	public static T Bind<T>(GameObject parent, string path) where T: Component
	{
		return Bind<T>(parent.transform, path);
	}
	
	[CanBeNull]
	public static T TryBind<T>(Transform parent, string path) where T: Component
	{
		Assert.IsNotNull(parent);
		Assert.IsFalse(string.IsNullOrEmpty(path));
		
		Transform child = parent.transform.Find(path);
		if (child == null)
		{
			return null;
		}
		Assert.IsNotNull(child);
		
		T comp = child.GetComponent<T>();
		if (comp == null)
		{
			return null;
		}
		Assert.IsNotNull(comp);
		
		return comp;
	}
	
	[CanBeNull]
	public static T TryBind<T>(GameObject parent, string path) where T : Component
	{
		return TryBind<T>(parent.transform, path);
	}
	
	public static T[] BindAll<T>(GameObject parent, string path) where T: Component
	{
		Assert.IsNotNull(parent);
		Assert.IsFalse(string.IsNullOrEmpty(path));
		
		Transform child = parent.transform.Find(path);
		if (child == null)
		{
			Debug.LogError($"bind target null, path:{parent.GetScenePath()}");
		}
		Assert.IsNotNull(child);
		if (child == null)
		{
			throw new Exception($"Failed to find a gameObject in path({path})");
		}
		
		T[] comp = child.GetComponentsInChildren<T>(true);
		Assert.IsNotNull(comp);
		if (comp == null)
		{
			throw new Exception($"Failed to find a component({typeof(T).Name}) from GameObject({path})");
		}
		
		return comp;
	}
	
	public static void TryBindAll<T>(GameObject parent, List<T> components) where T : Component
	{
		TryBindAll(parent, string.Empty, components);
	}
	
	public static void TryBindAll<T>(GameObject parent, string path, List<T> components) where T : Component
	{
		Assert.IsNotNull(parent);
		
		Transform child = string.IsNullOrEmpty(path) ? parent.transform : parent.transform.Find(path);
		if (child == null)
		{
			Debug.LogError($"bind target null, path:{parent.GetScenePath()}");
		}
		Assert.IsNotNull(child);
		if (child == null)
		{
			throw new Exception($"Failed to find a gameObject in path({path})");
		}
		
		child.GetComponentsInChildren(true, components);
	}
	
	public static Transform FindRecursively(this Transform Transform, string Name, out bool isFind, bool includeInactive = true)
	{
		isFind = true;
		
		Transform target = Transform.Find(Name);
		if (!ReferenceEquals(target, null))
		{
			if (includeInactive)
			{
				return target;
			}
			else if (target.gameObject.activeInHierarchy)
			{
				return target;
			}
		}
		
		for(int i = 0; i < Transform.childCount; i++)
		{
			target = Transform.GetChild(i).FindRecursively(Name, includeInactive);
			
			if (!ReferenceEquals(target, null))
			{
				return target;
			}
		}
		
		isFind = false;
		
		// Child가 없을 경우 null 반환
		return null;
	}
	
	public static Transform FindRecursively(this Transform Transform, string Name, bool includeInactive = true)
    {
		return FindRecursively(Transform, Name, out _, includeInactive);
    }
	
    // Hierarchy가 수정되었을 때 Bind의 path를 수정 해 주어야 함
    // 되도록이면 이 함수를 사용하는 것이 좋음
    // Bind 작업은 주로 Awake에서 이루어진다. Recursive Find는 비용이 비싸기 때문에 반드시 Awake나 Start에서 호출되어야 함
    public static T FindRecursivelyAndBind<T>(GameObject parent, string Name, bool includeInactive = true) where T: Component
    {
	    Transform result = null;
	    Transform parentTransform = parent.transform;
	    
	    // Transform 체크
	    if (parentTransform.name == Name)
	    {
		    if (includeInactive)
		    {
			    result = parentTransform;
		    }
		    else if (parent.activeInHierarchy)
		    {
			    result = parentTransform;
		    }
	    }
		
	    // Recursive로 부모 확인을 하지 않아도 되는 경우
	    foreach (Transform child in parentTransform)
	    {
		    Transform recursive = child.FindRecursively(Name, includeInactive);
		    if (recursive != null)
		    {
			    result = recursive;
		    }
	    }
		
	    if (result == null)
	    {
		    throw new Exception($"Failed to find a component({typeof(T).Name}) from GameObject({Name})");
	    }
	    
	    T comp = result.GetComponent<T>();
	    Assert.IsNotNull(comp);
	    if (comp == null)
	    {
		    throw new Exception($"Failed to find a component({typeof(T).Name}) from GameObject({Name})");
	    }
	    
	    return comp;
    }
    
    public static T[] FindRecursivelyAndBindAll<T>(GameObject parent, string Name, bool includeInactive = true) where T: Component
    {
	    Transform result = null;
	    Transform parentTransform = parent.transform;
	    
	    if (parentTransform.name == Name)
	    {
		    if (includeInactive)
		    {
			    result = parentTransform;
		    }
		    else if (parent.activeInHierarchy)
		    {
			    result = parentTransform;
		    }
	    }
		
	    foreach (Transform child in parentTransform)
	    {
		    Transform recursive = child.FindRecursively(Name, includeInactive);
		    if (recursive != null)
		    {
			    result = recursive;
		    }
	    }
		
	    if (result == null)
	    {
		    throw new Exception($"Failed to find a component({typeof(T).Name}) from GameObject({Name})");
	    }
	    
	    T[] comps = result.GetComponents<T>();
	    Assert.IsNotNull(comps);
	    if (comps == null)
	    {
		    throw new Exception($"Failed to find a component({typeof(T).Name}) from GameObject({Name})");
	    }
	    
	    return comps;
    }
	
    public static Transform FindParentRecursively(this Transform Transform, string Name)
    {
	    if (Transform == null)
	    {
		    return null;
	    }
	    
        if (Transform.name == Name)
        {
	        return Transform;
        }
		
        Transform recursive = Transform.parent.FindParentRecursively(Name);
        if (recursive != null)
        {
	        return recursive;
        }
		
        // Child가 없을 경우 null 반환
        return null;
    }
	
	private static System.Random rng = new System.Random();
	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = rng.Next(n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}
	
	public static int GetRandomElementalIndex(List<int> rateList)
	{
		if (rateList.Count == 0)
		{
			return -1;
		}
		
		int sumRate = rateList.Sum();
		
		int r = Random.Range(0, sumRate + 1);
		
		int rate = 0;
		for (int i = 0; i < rateList.Count; ++i)
		{
			rate += rateList[i];
			if (r <= rate)
			{
				return i;
			}
		}

		return -1;
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
	
	const double Epsilon = 0.0001; // 허용 오차
	public static bool IsEqual(double x, double y) // 비교 함수.
	{
		return (((x - Epsilon) < y) && (y < (x + Epsilon)));
	}
	
	public static void ExitApplication()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
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
	
	private static readonly Dictionary<int, string> intToStringMap = new Dictionary<int, string>();
	private const int maxIntToStringCacheValue = 21474836;
	private const int minIntToStringCacheValue = -21474836;
	
	public static string ToStringCached(this int intValue)
	{
		if (intValue > maxIntToStringCacheValue || intValue < minIntToStringCacheValue)
		{
			return intValue.ToString();
		}
		
		if (intToStringMap.TryGetValue(intValue, out var returnValue) == false)
		{
			returnValue = intValue.ToString();
			intToStringMap.Add(intValue, returnValue);
		}
		
		return returnValue;
	}
    
	private static readonly Dictionary<Enum, string> enumToStringMap = new Dictionary<Enum, string>();
	public static string ToStringCached(this Enum enumValue)
	{
		if (enumToStringMap.TryGetValue(enumValue, out var returnValue) == false)
		{
			returnValue = enumValue.ToString();
			enumToStringMap.Add(enumValue, returnValue);
		}
		
		return returnValue;
	}
	
    public static string GetScenePath(this GameObject obj)
	{
		string path = obj.name;
		Transform curPar = obj.transform.parent;
		while (curPar != null)
		{
			path = $"{curPar.gameObject.name}/{path}";
			curPar = curPar.parent;
		}
		return path;
	}
}