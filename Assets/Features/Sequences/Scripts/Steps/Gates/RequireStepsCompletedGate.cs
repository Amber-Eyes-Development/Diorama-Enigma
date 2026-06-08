using System;
using Extensions.Identification;
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
        [SequenceStepReference]
        [SerializeField] private IdentifiableObject[] requiredSteps;

        /// <inheritdoc/>
        public override bool IsSatisfied()
        {
            if (requiredSteps == null) return true;

            foreach (var required in requiredSteps)
            {
                if (required == null) continue;

                if (required is not ISequenceStep step) return false;

                if (!step.IsCompleted) return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void StartObserving()
        {
            if (requiredSteps == null) return;

            foreach (var required in requiredSteps)
                if (required is ISequenceStep step)
                    step.onCompletionChanged += OnRequiredCompletionChanged;
        }

        /// <inheritdoc/>
        public override void StopObserving()
        {
            if (requiredSteps == null) return;

            foreach (var required in requiredSteps)
                if (required is ISequenceStep step)
                    step.onCompletionChanged -= OnRequiredCompletionChanged;
        }

        private void OnRequiredCompletionChanged(bool _) => RaiseSatisfactionChanged();

#if UNITY_EDITOR
        public void EditorValidate(UnityEngine.Object context)
        {
            if (requiredSteps == null) return;

            foreach (var required in requiredSteps)
                if (required != null && required is not ISequenceStep)
                    Extensions.Log.ServiceDebug.LogWarning(context,
                        $"RequireStepsCompletedGate: «{required.name}» не реализует {nameof(ISequenceStep)}");
        }
#endif
    }
}
