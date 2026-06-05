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
        [Tooltip("Шаги, которые должны быть завершены для разблокировки. " +
                 "Принимаются только ассеты-шаги (IPuzzleStep)")]
        [SerializeField] private IdentifiableObject[] requiredSteps;

        /// <inheritdoc/>
        public override bool IsSatisfied()
        {
            if (requiredSteps == null) return true;

            foreach (var required in requiredSteps)
            {
                if (required == null) continue; // пустой слот — пропускаем

                // Некорректная ссылка (не шаг) блокирует гейт, а не пропускается
                if (required is not IPuzzleStep step) return false;

                if (!step.IsCompleted) return false;
            }

            return true;
        }

#if UNITY_EDITOR
        public void EditorValidate(UnityEngine.Object context)
        {
            if (requiredSteps == null) return;

            foreach (var required in requiredSteps)
                if (required != null && required is not IPuzzleStep)
                    Extensions.Log.ServiceDebug.LogWarning(context,
                        $"RequireStepsCompletedGate: «{required.name}» не реализует IPuzzleStep");
        }
#endif
    }
}
