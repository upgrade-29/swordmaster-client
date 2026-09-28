using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T inst => Instance;
    
    public static T Instance
    {
        get
        {
            InitInstance();
            return _instance;
        }
    }
    
    public static bool IsInitialize()
    {
        return _instance;
    }
    
    public static void InitInstance()
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            return;
        }
#endif
        if (_instance == null)
        {
            GameObject emptyGameObject = new GameObject($"{typeof(T)} (MonoSingleton)");
            _instance = emptyGameObject.AddComponent<T>();
        }
    }
    
    protected virtual void OnAwakeSingleton()
    {
    }
    
    protected virtual void OnDestroySingleton()
    {
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        OnAwakeSingleton();
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            OnDestroySingleton();
        }
    }
}