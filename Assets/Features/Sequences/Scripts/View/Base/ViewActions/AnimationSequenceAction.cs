using System;
using Extensions.AnimationSequencer;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: проигрывание <see cref="AnimationSequencer"/>
    /// </summary>
    [Serializable]
    public sealed class AnimationSequenceAction : ViewAction
    {
        /// <summary> Целевая последовательность анимаций </summary>
        public AnimationSequencer Target => target;
        /// <summary> Действие при срабатывании триггера </summary>
        public AnimationCommand Command => command;
        /// <summary> Действие на откате (если включён reversible) </summary>
        public AnimationReverseMode ReverseMode => reverseMode;

        public override void EnsureTarget(GameObject host)
        {
            if (target == null)
                target = host.GetComponent<AnimationSequencer>() ?? host.AddComponent<AnimationSequencer>();
        }

        [Tooltip("Целевой AnimationSequencer")]
        [SerializeField] private AnimationSequencer target;
        [Tooltip("Действие при срабатывании триггера")]
        [SerializeField] private AnimationCommand command;
        [Tooltip("Действие на откате (если включён reversible)")]
        [SerializeField] private AnimationReverseMode reverseMode;
    }
}
