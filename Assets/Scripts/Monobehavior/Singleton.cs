using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<T>();

            if (instance == null)
            {
                GameObject newInstance = new GameObject();
                newInstance.name = typeof(T).Name;
                instance = newInstance.AddComponent<T>();
                Debug.LogWarning($"Could not find {typeof(T).Name}, new instance was created");
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.LogWarning($"Duplicate instance of {typeof(T).Name} was destroyed");
        }
        else
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}