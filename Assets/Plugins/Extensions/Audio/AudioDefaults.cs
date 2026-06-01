using System;
using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.Audio;

namespace Extensions.Audio
{
    /// <summary>
    /// Дефолтные параметры аудио трека
    /// </summary>
    [Serializable]
    public struct AudioDefaults
    {
        /// <summary>
        /// Миксер-группа
        /// </summary>
        [Tooltip("Миксер-группа")]
        public AudioMixerGroup mixerGroup;

        /// <summary>
        /// Модификатор базовой громкости
        /// </summary>
        [Tooltip("Модификатор базовой громкости")]
        [Range(0f, 1f)]
        public float volumeModifier;

        /// <summary>
        /// Громкость
        /// </summary>
        [Tooltip("Громкость")]
        public FloatValue volume;


        /// <summary>
        /// Длительность затухания
        /// </summary>
        [Tooltip("Длительность затухания")]
        [Min(0f)]
        public float fadeSeconds;

        /// <summary>
        /// Максимальная высота (скорость)
        /// </summary>
        [Tooltip("Максимальная высота (скорость)")]
        [Range(-3f, 3f)]
        public float pitchMin;
        /// <summary>
        /// Минимальная высота (скорость)
        /// </summary>
        [Tooltip("Минимальная высота (скорость)")]
        [Range(-3f, 3f)]
        public float pitchMax;

        /// <summary>
        /// Пресет настроек объемного аудио
        /// </summary>
        [Tooltip("Пресет настроек объемного аудио")]
        public AudioSpatialPreset spatialPreset;

        /// <summary>
        /// Приоритет
        /// </summary>
        [Tooltip("Приоритет")]
        [Range(0, 256)]
        public int priority;
    }
}
