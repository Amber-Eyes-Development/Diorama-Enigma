using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Один шаг пазла: набор условий (AND-логика) и эффекты при активации/завершении/провале.
    /// Хранится инлайн внутри <see cref="PuzzleSequence"/> — отдельным ассетом не является.
    /// </summary>
    [Serializable]
    public sealed class PuzzleStep
    {
        /// <summary>Метка для отображения в редакторе</summary>
        public string StepLabel => stepLabel;

        /// <summary>Условия шага (все должны выполниться)</summary>
        public IReadOnlyList<PuzzleCondition> Conditions => conditions;

        /// <summary>Эффекты, выполняемые при активации шага (до ожидания условий)</summary>
        public IReadOnlyList<PuzzleEffect> ActivationEffects => activationEffects;

        /// <summary>Эффекты, выполняемые при завершении шага</summary>
        public IReadOnlyList<PuzzleEffect> Effects => effects;

        /// <summary>Эффекты, выполняемые при провале шага</summary>
        public IReadOnlyList<PuzzleEffect> FailureEffects => failureEffects;

        [SerializeField] private string stepLabel;
        [SerializeReference] private PuzzleCondition[] conditions = Array.Empty<PuzzleCondition>();
        [Tooltip("Выполняются сразу при активации шага — до ожидания условий. " +
                 "Используйте для разблокировки объектов (SetInteractableLockEffect).")]
        [SerializeReference] private PuzzleEffect[] activationEffects = Array.Empty<PuzzleEffect>();
        [SerializeReference] private PuzzleEffect[] effects = Array.Empty<PuzzleEffect>();
        [SerializeReference] private PuzzleEffect[] failureEffects = Array.Empty<PuzzleEffect>();
    }
}
