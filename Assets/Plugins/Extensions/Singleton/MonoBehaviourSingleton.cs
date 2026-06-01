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

        /// <summary>
        /// Инстанс синглтона
        /// </summary>
        public static T Instance
        {
            get
            {
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
    }
}