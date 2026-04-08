using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;

    [Header("Singleton")]
    [SerializeField] private bool canOverwrite = false;
    [SerializeField] private bool isDestroyedOnLoad = false;

    public static T Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<T>();
            if (instance != null)
            {
                return instance;
            }

            GameObject singletonObject = new(typeof(T).Name);
            instance = singletonObject.AddComponent<T>();
            Debug.LogWarning($"No {typeof(T).Name} was found in the scene, so a new one was created automatically.");
            return instance;
        }
    }

    protected virtual void Awake()
    {
        T current = this as T;

        if (instance != null && instance != current)
        {
            if (!canOverwrite)
            {
                Debug.LogWarning($"Duplicate instance of {typeof(T).Name} on {name} was destroyed.");
                Destroy(gameObject);
                return;
            }

            if (instance.gameObject != null)
            {
                Destroy(instance.gameObject);
            }
        }

        instance = current;

        if (!isDestroyedOnLoad)
        {
             DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance != null)
        {
            instance = null;
        }
    }
}
