using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Гейт: требует завершения указанных шагов (в т.ч. из другой последовательности)
    /// </summary>
    [Serializable]
    public sealed class RequireStepsCompletedGate : StepGate
    {
        [Tooltip("Шаги, которые должны быть завершены для разблокировки")]
        [SerializeField] private AbstractSequenceStep[] requiredSteps;

        /// <inheritdoc/>
        public override bool IsSatisfied()
        {
            if (requiredSteps == null) return true;

            foreach (var step in requiredSteps)
            {
                if (step == null) continue;
                if (!step.IsCompleted) return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void StartObserving()
        {
            if (requiredSteps == null) return;

            foreach (var step in requiredSteps)
                if (step != null)
                    step.onCompletionChanged += OnRequiredCompletionChanged;
        }

        /// <inheritdoc/>
        public override void StopObserving()
        {
            if (requiredSteps == null) return;

            foreach (var step in requiredSteps)
                if (step != null)
                    step.onCompletionChanged -= OnRequiredCompletionChanged;
        }

        private void OnRequiredCompletionChanged(bool _) => RaiseSatisfactionChanged();
    }
}
