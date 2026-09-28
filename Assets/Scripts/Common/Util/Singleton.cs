public abstract class Singleton<T> where T : Singleton<T>, new()
{
    private static T _instance;
    public static T inst => Instance;
    
    public static T Instance
    {
        get
        {
            return _instance;
        }
    }
    
    protected virtual void OnClear()
    {
    }
    
    protected virtual void OnCreate()
    {
    }
    
    static Singleton()
    {
        if (_instance == null)
        {
            _instance = new T();
            _instance.OnCreate();
        }
    }
    
    public void Clear()
    {
        OnClear();
        _instance = null;
    }
}