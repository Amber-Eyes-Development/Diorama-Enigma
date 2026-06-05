using System;
using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Гейт: требует завершения указанных шагов (в т.ч. из другой последовательности)
    /// </summary>
    [Serializable]
    public sealed class RequireStepsCompletedGate : StepGate
    {
        [Tooltip("Шаги, которые должны быть завершены для разблокировки изменения состояния. " +
                 "Ссылки на ассеты-шаги, в т.ч. из другой последовательности")]
        [SerializeField] private IdentifiableObject[] requiredSteps;

        /// <inheritdoc/>
        public override bool IsSatisfied()
        {
            if (requiredSteps == null) return true;

            foreach (var required in requiredSteps)
            {
                if (required is not IPuzzleStep step) continue;
                if (!step.IsCompleted) return false;
            }

            return true;
        }
    }
}
