using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Последовательность шагов пазла с поддержкой параллельных групп.
    /// Единственный ассет системы — шаги, условия и эффекты хранятся инлайн.
    /// Шаги с одинаковым <see cref="StepEntry.GroupIndex"/> активируются одновременно;
    /// следующая группа стартует только после завершения всей текущей.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/PuzzleSequence", fileName = nameof(PuzzleSequence))]
    public sealed class PuzzleSequence : ScriptableObject
    {
        /// <summary>Метка для отображения в редакторе и в событиях</summary>
        public string SequenceLabel => sequenceLabel;

        /// <summary>Все шаги последовательности</summary>
        public IReadOnlyList<StepEntry> Steps => steps;

        [SerializeField] private string sequenceLabel;
        [SerializeField] private StepEntry[] steps;

        /// <summary>Шаг последовательности с индексом параллельной группы</summary>
        [Serializable]
        public sealed class StepEntry
        {
            [SerializeReference] public PuzzleStep Step;

            [Tooltip("Шаги с одинаковым GroupIndex активируются одновременно (параллельно). " +
                     "Следующая группа стартует только после завершения всех шагов текущей.")]
            public int GroupIndex;
        }
    }
}
