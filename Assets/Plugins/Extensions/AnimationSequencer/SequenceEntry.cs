using System;
using DG.Tweening;
using UnityEngine;

namespace Extensions.AnimationSequencer
{
    /// <summary>
    /// Одна запись в последовательности <see cref="AnimationSequencer"/> — ссылка на DOTweenAnimation и режим добавления
    /// </summary>
    [Serializable]
    public sealed class SequenceEntry
    {
        /// <summary> Компонент анимации </summary>
        public DOTweenAnimation Animation => animation;

        /// <summary> Запустить параллельно с предыдущим шагом (.Join), иначе последовательно (.Append) </summary>
        public bool JoinWithPrevious => joinWithPrevious;

        [Tooltip("Компонент DOTweenAnimation, которым управляет этот шаг")]
        [SerializeField] private DOTweenAnimation animation;

        [Tooltip("Если включено — шаг запускается параллельно с предыдущим (.Join)")]
        [SerializeField] private bool joinWithPrevious;
    }
}
