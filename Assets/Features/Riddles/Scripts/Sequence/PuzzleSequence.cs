using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Последовательность шагов пазла с поддержкой параллельных групп.
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
        public struct StepEntry
        {
            /// <summary>Шаг пазла</summary>
            public PuzzleStep Step;

            [Tooltip("Шаги с одинаковым GroupIndex активируются одновременно (параллельно). " +
                     "Следующая группа стартует только после завершения всех шагов текущей.")]
            /// <summary>Индекс группы параллельного выполнения</summary>
            public int GroupIndex;
        }
    }
}
