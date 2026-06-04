using System.Collections;
using System.Collections.Generic;
using Extensions.Pool;
using UnityEngine;

namespace DioramaEnigma.Tactility
{
    /// <summary>
    /// Пул партикл-эффектов для тактильной связи по клику на объект
    /// </summary>
    public sealed class SurfaceParticlePool : MonoBehaviour
    {
        [Tooltip("Сколько экземпляров создавать заранее на каждый префаб")]
        [Min(0)]
        [SerializeField] private int prewarmPerPrefab = 0;

        [Tooltip("Максимальный размер пула на каждый префаб (0 — без ограничений)")]
        [Min(0)]
        [SerializeField] private int maxSizePerPrefab = 16;

        private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> pools = new();

        /// <summary>
        /// Заспавнить эффект в точке по направлению нормали
        /// </summary>
        /// <param name="prefab">Префаб эффекта</param>
        /// <param name="position">Точка соприкосновения</param>
        /// <param name="normal">Нормаль поверхности (направление эффекта)</param>
        public void Play(ParticleSystem prefab, Vector3 position, Vector3 normal)
        {
            if (prefab == null) return;

            ObjectPool<ParticleSystem> pool = GetPool(prefab);
            ParticleSystem instance = pool.Get();
            if (instance == null) return;

            instance.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
            instance.Clear(true);
            instance.Play(true);

            StartCoroutine(ReleaseWhenDone(instance, pool));
        }

        #region Internal

        private ObjectPool<ParticleSystem> GetPool(ParticleSystem prefab)
        {
            if (pools.TryGetValue(prefab, out ObjectPool<ParticleSystem> pool)) return pool;

            pool = new ObjectPool<ParticleSystem>(prefab, transform, prewarmPerPrefab, maxSizePerPrefab);
            pools.Add(prefab, pool);
            return pool;
        }

        private static IEnumerator ReleaseWhenDone(ParticleSystem instance, ObjectPool<ParticleSystem> pool)
        {
            while (instance != null && instance.IsAlive(true))
                yield return null;

            if (instance != null) pool.Release(instance);
        }

        #endregion
    }
}
