using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Гейт (условия доступности <see cref="SequenceStep"/> к измнению состояния)
    /// </summary>
    [Serializable]
    public sealed class RequireStepsCompletedGate : StepGate
    {
        [Tooltip("Состояние шагов, при котором гейт считается пройденным")]
        [SerializeField] private TriggerKind trigger = TriggerKind.Completed;
        [Tooltip("Шаги, которые должны быть в нужном состоянии для разблокировки")]
        [SerializeField] private AbstractSequenceStep[] requiredSteps;

        /// <inheritdoc/>
        public override bool IsSatisfied()
        {
            if (requiredSteps == null) return true;

            foreach (var step in requiredSteps)
            {
                if (step == null) continue;
                if (!Matches(step)) return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void StartObserving()
        {
            if (requiredSteps == null) return;

            foreach (var step in requiredSteps)
            {
                if (step == null) continue;
                step.onCompletionChanged += OnRequiredStateChanged;
                step.onUnlockChanged += OnRequiredStateChanged;
            }
        }

        /// <inheritdoc/>
        public override void StopObserving()
        {
            if (requiredSteps == null) return;

            foreach (var step in requiredSteps)
            {
                if (step == null) continue;
                step.onCompletionChanged -= OnRequiredStateChanged;
                step.onUnlockChanged -= OnRequiredStateChanged;
            }
        }

        private bool Matches(AbstractSequenceStep step) =>
            trigger.IsChange() || trigger.IsSatisfiedBy(step.IsCompleted, step.IsUnlocked);

        private void OnRequiredStateChanged(bool _) => RaiseSatisfactionChanged();
    }
}
