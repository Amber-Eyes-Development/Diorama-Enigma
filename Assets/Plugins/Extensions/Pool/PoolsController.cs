using System.Collections.Generic;
using Extensions.Singleton;
using Unity.VisualScripting;
using UnityEngine;

namespace Extensions.Pool
{
    /// <summary>
    /// Опциональный фасад, управляющий множеством пулов
    /// </summary>
    /// <remarks>
    /// Используется там, где нужен Spawn/Despawn без знания пула
    /// </remarks>
    public class PoolsController : MonoBehaviourSingleton<PoolsController>
    {
        /// <summary>
        /// Корень свободных инстансов пулов
        /// </summary>
        [SerializeField]
        [Tooltip("Корень свободных инстансов пулов")]
        protected Transform poolRoot = null;

        /// <summary>
        /// Количество экземпляров для создания при инициализации пула
        /// </summary>
        [SerializeField]
        [Min(0)]
        [Tooltip("Количество экземпляров для создания при инициализации пула")]
        protected int defaultPrewarm = 0;

        /// <summary>
        /// Максимальное количество инстансов в пуле
        /// </summary>
        [SerializeField]
        [Min(0)]
        [Tooltip("Максимальное количество инстансов в пуле")]
        protected int defaultMaxSize = 0;

        /// <summary>
        /// Нужно ли автоматически добавлять PooledObject на инстансы
        /// </summary>
        [SerializeField]
        [Tooltip("Нужно ли автоматически добавлять PooledObject на инстансы")]
        protected bool markSpawnedInstances = true;

        protected readonly Dictionary<int, IPool> pools = new Dictionary<int, IPool>();

        protected override void Awake()
        {
            base.Awake();

            if (poolRoot == null)
            {
                poolRoot = transform;
            }
        }

        /// <summary>
        /// Создать свободный инстанс или создать новый при отсутствии свободных
        /// </summary>
        /// <param name="prefab">Префаб объекта</param>
        /// <param name="parent">Корень (родитель) инстанса</param>
        /// <typeparam name="T">Тип префаба объекта</typeparam>
        /// <returns></returns>
        public T Spawn<T>(T prefab, Transform parent = null) where T : Component
        {
            if (prefab == null) return null;

            IPool pool = GetOrCreatePool(prefab);
            Component instance = pool.Get(parent);

            return instance as T;
        }

        /// <summary>
        /// Отпустить активный инстанс обратно в пул (сделать свободным)
        /// </summary>
        /// <param name="pooled">Инстанс объекта</param>
        public void Despawn(PooledObject pooled)
        {
            if (pooled == null) return;

            int id = pooled.PrefabId;
            if (!pools.ContainsKey(id)) return;

            IPool pool = pools[id];
            pool.Release(pooled.GetComponent<Component>());
        }

        /// <summary>
        /// Заготовка инстансов (спавн свободных объектов) 
        /// </summary>
        /// <param name="prefab">Префаб объекта</param>
        /// <param name="count">Количество инстансов для заготовки</param>
        /// <typeparam name="T">Тип префаба объекта</typeparam>
        public void Prewarm<T>(T prefab, int count) where T : Component
        {
            ObjectPool<T> pool = GetOrCreateTypedPool(prefab);
            pool.Prewarm(count);
        }

        #region Internal
        
        protected IPool GetOrCreatePool<T>(T prefab) where T : Component
        {
            int id = prefab.gameObject.GetInstanceID();

            if (pools.ContainsKey(id))
            {
                return pools[id];
            }

            ObjectPool<T> pool = new ObjectPool<T>(
                prefab,
                poolRoot,
                defaultPrewarm,
                defaultMaxSize,
                this,
                markSpawnedInstances
            );

            pools.Add(id, pool);
            return pool;
        }

        protected ObjectPool<T> GetOrCreateTypedPool<T>(T prefab) where T : Component
        {
            int id = prefab.gameObject.GetInstanceID();

            if (pools.ContainsKey(id))
            {
                return pools[id] as ObjectPool<T>;
            }

            ObjectPool<T> pool = new ObjectPool<T>(
                prefab,
                poolRoot,
                defaultPrewarm,
                defaultMaxSize,
                this,
                markSpawnedInstances
            );

            pools.Add(id, pool);
            return pool;
        }
        
        #endregion
    }
}
