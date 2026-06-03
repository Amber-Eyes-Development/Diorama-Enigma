using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Последовательность шагов пазла
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/PuzzleSequence", fileName = nameof(PuzzleSequence))]
    public sealed class PuzzleSequence : ScriptableObject
    {
        /// <summary> Метка последовательности </summary>
        public string SequenceLabel => sequenceLabel;

        /// <summary> Шаги последовательности </summary>
        public IReadOnlyList<StepEntry> Steps => steps;

        [SerializeField] private string sequenceLabel;
        [SerializeField] private StepEntry[] steps;

        /// <summary> Шаг с индексом параллельной группы </summary>
        [Serializable]
        public sealed class StepEntry
        {
            [SerializeReference] public PuzzleStep Step;

            [Tooltip("Шаги с одинаковым GroupIndex активируются одновременно. " +
                     "Следующая группа стартует после завершения текущей")]
            public int GroupIndex;
        }
    }
}
