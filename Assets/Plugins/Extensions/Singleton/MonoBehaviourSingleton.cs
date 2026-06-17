using UnityEngine;

namespace Extensions.Singleton
{
    /// <summary>
    /// MonoBehaviour-синглтон с проверкой на экземпляр
    /// </summary>
    public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Header("Синглтон"), Space]
        [SerializeField] protected bool dontDestroyOnLoad = false;
        
        private static T _instance;
        private static bool applicationIsQuitting;

        /// <summary>
        /// Инстанс синглтона
        /// </summary>
        public static T Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                applicationIsQuitting = false;
                if (dontDestroyOnLoad)
                {
                    gameObject.transform.parent = null;
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit() => applicationIsQuitting = true;
    }
}