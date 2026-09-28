using System;
using System.Collections.Generic;

public abstract class SingletonComparerEnum<T, TCompare> : IEqualityComparer<TCompare>
    where T : class, new()
    where TCompare : Enum
{
    public static T Instance
    {
        get;  
        private set;  
    }

    static SingletonComparerEnum()
    {
        if (Instance == null)
        {
            Instance = new T();  
        }
    }

    public abstract bool Equals(TCompare x, TCompare y);

    public abstract int GetHashCode(TCompare obj);
}  



public abstract class SingletonComparerClass<T, TCompare> : IEqualityComparer<TCompare>
    where T : class, new()
    where TCompare : class
{
    public static T Instance
    {
        get;  
        private set;  
    }

    static SingletonComparerClass()
    {
        if (Instance == null)
        {
            Instance = new T();  
        }
    }

    public abstract bool Equals(TCompare x, TCompare y);

    public abstract int GetHashCode(TCompare obj);
}  


public abstract class SingletonComparerStruct<T, TCompare> : IEqualityComparer<TCompare>
    where T : class, new()
    where TCompare : struct
{
    public static T Instance
    {
        get;  
        private set;  
    }

    static SingletonComparerStruct()
    {
        if (Instance == null)
        {
            Instance = new T();  
        }
    }

    public abstract bool Equals(TCompare x, TCompare y);

    public abstract int GetHashCode(TCompare obj);
}