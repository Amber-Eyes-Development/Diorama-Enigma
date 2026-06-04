using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Tactility
{
    /// <summary>
    /// База соответствий «физическая поверхность → звук клика» для тактильности по клику по объекту
    /// </summary>
    [CreateAssetMenu(menuName = "Tactility/Surface Sound Database", fileName = nameof(SurfaceSoundDatabase))]
    public sealed class SurfaceSoundDatabase : ScriptableObject
    {
        [Tooltip("Соответствия материалов поверхностей и звуков клика")]
        [SerializeField] private SurfaceSoundPair[] entries;

        [Tooltip("Звук по-умолчанию: для коллайдеров без материала или без записи в базе")]
        [SerializeField] private AudioResource defaultSound;

        private Dictionary<PhysicsMaterial, AudioResource> lookup;

        /// <summary> Подобрать звук для поверхности (или дефолтный) </summary>
        /// <param name="material"> Физический материал коллайдера (может быть null) </param>
        /// <returns> Звук для воспроизведения или null, если ничего не назначено </returns>
        public AudioResource Resolve(PhysicsMaterial material)
        {
            if (material == null) return defaultSound;

            EnsureLookup();
            return lookup.TryGetValue(material, out AudioResource sound) ? sound : defaultSound;
        }

        private void OnEnable() => lookup = null;
        private void OnDisable() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<PhysicsMaterial, AudioResource>();
            if (entries == null) return;

            foreach (SurfaceSoundPair entry in entries)
            {
                if (entry.Material == null) continue;
                lookup[entry.Material] = entry.Sound;
            }
        }
    }
}
