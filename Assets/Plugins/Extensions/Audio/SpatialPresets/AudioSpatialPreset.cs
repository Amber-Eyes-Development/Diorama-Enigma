using UnityEngine;

namespace Extensions.Audio
{
    /// <summary>
    /// Пресет настроек объемного аудио
    /// </summary>
    [CreateAssetMenu(menuName = "Extensions/Audio/" + nameof(AudioSpatialPreset))]
    public class AudioSpatialPreset : ScriptableObject
    {
        /// <summary>
        /// Объемность аудио
        /// </summary>
        public float SpatialBlend => spatialBlend;
        /// <summary>
        /// Расстояние, ближе которого громкость не растёт (радиус максимальной громкости)
        /// </summary>
        public float MinDistance => minDistance;
        /// <summary>
        /// Расстояние, дальше которого звук почти не слышен (максимальный радиус слышимости)
        /// </summary>
        public float MaxDistance => maxDistance;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Пространственность звука, 0 — 2D, 1 — 3D")]
        protected float spatialBlend = 1f;
        [SerializeField, Min(0f)]
        [Tooltip("Расстояние, ближе которого громкость не растёт (радиус максимальной громкости)")]
        protected float minDistance = 10f;
        [SerializeField, Min(0f)]
        [Tooltip("Расстояние, дальше которого звук почти не слышен (максимальный радиус слышимости)")]
        protected float maxDistance = 20f;
    }
}