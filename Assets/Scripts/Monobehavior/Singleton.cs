using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Singleton")]
    [SerializeField] private bool _canOverwrite = false;
    [SerializeField] private bool _isDestroyedOnLoad = false;

    protected static T _instance;


    public static T Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<T>();
            if (_instance != null)
            {
                return _instance;
            }

            GameObject singletonObject = new(typeof(T).Name);
            _instance = singletonObject.AddComponent<T>();
            Debug.LogWarning($"No {typeof(T).Name} was found in the scene, so a new one was created automatically.");
            return _instance;
        }
    }


    protected virtual void Awake()
    {
        T current = this as T;

        if (_instance != null && _instance != current)
        {
            if (!_canOverwrite)
            {
                Debug.LogWarning($"Duplicate instance of {typeof(T).Name} on {name} was destroyed.");
                Destroy(gameObject);
                return;
            }

            if (_instance.gameObject != null)
            {
                Destroy(_instance.gameObject);
            }
        }

        _instance = current;

        if (!_isDestroyedOnLoad)
        {
             DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance != null)
        {
            _instance = null;
        }
    }
}
