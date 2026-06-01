using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Pool
{
    /// <summary>
    /// Пул однотипных объектов
    /// </summary>
    /// <typeparam name="T">Тип хранимого объекта</typeparam>
    public class ObjectPool<T> : IPool where T : Component
    {
        protected const int DEFAULT_PREWARM_INSTANCES_NUMBER = 0;
        protected const int DEFAULT_MAX_POOL_SIZE = 0; // 0 = без ограничений
        protected const int WARNING_PREWARM_NUMBER = 300;
        
        /// <summary>
        /// Количество свободных объектов
        /// </summary>
        public int CountAvailable => available.Count;
        /// <summary>
        /// Количество активных объектов
        /// </summary>
        public int CountInUse => inUse.Count;
        /// <summary>
        /// Количество всех (свободных и активных) объектов пула
        /// </summary>
        public int CountAll => available.Count + inUse.Count;
        
        /// <summary>
        /// Объект, хранимый пулом
        /// </summary>
        protected readonly T prefab;
        /// <summary>
        ///  Корень (родитель) неактивных объектов пула
        /// </summary>
        protected readonly Transform root;

        /// <summary>
        /// Свободные объекты пула
        /// </summary>
        protected readonly Stack<T> available;
        /// <summary>
        /// Активные объекты пула
        /// </summary>
        protected readonly HashSet<T> inUse;

        /// <summary>
        /// Максимальный размер пула
        /// </summary>
        protected readonly int maxSize;

        #region  Компоненты для PoolCOntroller 
        
        /// <summary>
        /// Флаг инфраструктуры — навешивать ли PooledObject
        /// </summary>
        protected readonly bool markInstances;
        /// <summary>
        /// Владелец пула
        /// </summary>
        protected readonly PoolsController owner;
        /// <summary>
        /// Идентификатор инстанса префаба
        /// </summary>
        protected readonly int prefabId;
        
        #endregion

        /// <summary>
        /// Конструктор пула
        /// </summary>
        /// <param name="prefab">Объект, хранимый пулом</param>
        /// <param name="root">Корень (родитель) неактивных инстансов пула</param>
        /// <param name="prewarm">Количество экземпляров для создания при инициализации пула</param>
        /// <param name="maxSize">Максимальное количество инстансов в пуле</param>
        /// <param name="owner">Владелец пула (контроллер пулов)</param>
        /// <param name="markInstances">Нужно ли автоматически добавлять PooledObject на инстансы</param>
        public ObjectPool(
            T prefab,
            Transform root,
            int prewarm = DEFAULT_PREWARM_INSTANCES_NUMBER,
            int maxSize = DEFAULT_MAX_POOL_SIZE,
            PoolsController owner = null,
            bool markInstances = false)
        {
            
            this.prefab = prefab;
            this.root = root != null ? root : owner != null ? owner.transform : null;
            this.maxSize = maxSize;

            this.owner = owner;
            this.markInstances = markInstances;

            prefabId = prefab != null ? prefab.gameObject.GetInstanceID() : 0;

            available = new Stack<T>(Mathf.Max(0, prewarm));
            inUse = new HashSet<T>();
            
            if (prewarm >= WARNING_PREWARM_NUMBER)
            {
                ServiceDebug.LogWarning($"Подготовка слишком большого количества ({prewarm}), рекомендуется уменьшить количество");
            }
            Prewarm(prewarm);
        }

        /// <summary>
        /// Заготовка инстансов (спавн свободных объектов) 
        /// </summary>
        /// <param name="count"></param>
        public void Prewarm(int count)
        {
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                T instance = CreateInstance();
                ReleaseInternal(instance);
            }
        }

        /// <summary>
        /// Получить свободный инстанс или создать новый при отсутствии свободных
        /// </summary>
        /// <param name="parent">Корень (родитель) инстанса</param>
        /// <returns></returns>
        public T Get(Transform parent = null)
        {
            T instance = null;

            while (available.Count > 0 && instance == null)
            {
                instance = available.Pop();
            }

            if (instance == null)
            {
                if (maxSize > 0 && CountAll >= maxSize)
                {
                    return null;
                }

                instance = CreateInstance();
            }

            inUse.Add(instance);

            Transform tr = instance.transform;
            tr.SetParent(parent, false);

            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Отпустить активный инстанс обратно в пул (сделать свободным)
        /// </summary>
        /// <param name="instance"></param>
        public void Release(T instance)
        {
            if (instance == null) return;
            if (!inUse.Remove(instance)) return;

            ReleaseInternal(instance);
        }

        #region Internal
        
        protected void ReleaseInternal(T instance)
        {
            Transform tr = instance.transform;
            tr.SetParent(root, false);

            instance.gameObject.SetActive(false);
            available.Push(instance);
        }

        protected T CreateInstance()
        {
            if (prefab == null) return null;

            T instance = Object.Instantiate(prefab, root);
            instance.gameObject.SetActive(false);

            if (markInstances)
            {
                PooledObject pooled = instance.GetComponent<PooledObject>();
                if (pooled == null)
                {
                    pooled = instance.gameObject.AddComponent<PooledObject>();
                }

                pooled.Initialize(owner, prefabId);
            }

            return instance;
        }

        Component IPool.Get(Transform parent)
        {
            return Get(parent);
        }

        void IPool.Release(Component instance)
        {
            T typed = instance as T;
            if (typed == null) return;

            Release(typed);
        }

        bool IPool.Owns(Component instance)
        {
            T typed = instance as T;
            if (typed == null) return false;

            return inUse.Contains(typed) || available.Contains(typed);
        }
        
        #endregion
    }
}
