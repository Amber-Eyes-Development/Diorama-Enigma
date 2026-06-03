using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Данные шага пазла: условия и эффекты
    /// </summary>
    [Serializable]
    public sealed class PuzzleStep
    {
        /// <summary> Метка шага </summary>
        public string StepLabel => stepLabel;

        /// <summary> Условия (AND) </summary>
        public IReadOnlyList<PuzzleCondition> Conditions => conditions;

        /// <summary> Эффекты при активации </summary>
        public IReadOnlyList<PuzzleEffect> ActivationEffects => activationEffects;

        /// <summary> Эффекты при завершении </summary>
        public IReadOnlyList<PuzzleEffect> Effects => effects;

        /// <summary> Эффекты при провале </summary>
        public IReadOnlyList<PuzzleEffect> FailureEffects => failureEffects;

        [SerializeField] private string stepLabel;
        [SerializeReference] private PuzzleCondition[] conditions = Array.Empty<PuzzleCondition>();
        [Tooltip("Выполняются при активации шага — до ожидания условий")]
        [SerializeReference] private PuzzleEffect[] activationEffects = Array.Empty<PuzzleEffect>();
        [SerializeReference] private PuzzleEffect[] effects = Array.Empty<PuzzleEffect>();
        [SerializeReference] private PuzzleEffect[] failureEffects = Array.Empty<PuzzleEffect>();
    }
}
