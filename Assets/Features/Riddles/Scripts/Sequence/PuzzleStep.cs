using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Один шаг пазла: набор условий (AND-логика) и эффекты при завершении.
    /// Используется как элемент <see cref="PuzzleSequence"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/PuzzleStep", fileName = nameof(PuzzleStep))]
    public sealed class PuzzleStep : ScriptableObject
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
        [SerializeField] private PuzzleCondition[] conditions;
        [Tooltip("Выполняются сразу при активации шага — до ожидания условий. " +
                 "Используйте для разблокировки объектов (SetInteractableLockEffect).")]
        [SerializeField] private PuzzleEffect[] activationEffects;
        [SerializeField] private PuzzleEffect[] effects;
        [SerializeField] private PuzzleEffect[] failureEffects;
    }
}
