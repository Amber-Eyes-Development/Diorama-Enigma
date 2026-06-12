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

        public override void EnsureTarget(GameObject host)
        {
            if (target == null)
                target = host.GetComponent<DOTweenAnimation>() ?? host.AddComponent<DOTweenAnimation>();
        }

        public override void PrepareTarget()
        {
            if (target == null) return;

            target.autoGenerate = false;
            target.autoPlay = false;
            target.autoKill = false;
        }

        [Tooltip("Целевая DOTweenAnimation")]
        [SerializeField] private DOTweenAnimation target;
        [Tooltip("Действие при срабатывании триггера")]
        [SerializeField] private AnimationCommand command;
        [Tooltip("Действие на откате (если включён reversible)")]
        [SerializeField] private AnimationReverseMode reverseMode;
    }
}
