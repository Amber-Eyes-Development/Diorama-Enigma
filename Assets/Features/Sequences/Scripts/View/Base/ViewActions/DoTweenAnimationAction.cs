using System;
using DG.Tweening;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: проигрывание <see cref="DOTweenAnimation"/>
    /// </summary>
    [Serializable]
    public sealed class DoTweenAnimationAction : ViewAction
    {
        /// <summary> Целевая анимация </summary>
        public DOTweenAnimation Target => target;
        /// <summary> Действие при срабатывании триггера </summary>
        public AnimationCommand Command => command;
        /// <summary> Действие на откате (если включён reversible) </summary>
        public AnimationReverseMode ReverseMode => reverseMode;

        [Tooltip("Целевая DOTweenAnimation")]
        [SerializeField] private DOTweenAnimation target;
        [Tooltip("Действие при срабатывании триггера")]
        [SerializeField] private AnimationCommand command;
        [Tooltip("Действие на откате (если включён reversible)")]
        [SerializeField] private AnimationReverseMode reverseMode;
    }
}
