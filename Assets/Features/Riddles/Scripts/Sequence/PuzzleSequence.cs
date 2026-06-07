using System;
using System.Collections.Generic;
using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Последовательность шагов загадки: упорядоченные группы ссылок на ассеты-шаги
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/PuzzleSequence", fileName = nameof(PuzzleSequence))]
    public sealed class PuzzleSequence : ScriptableObject
    {
        /// <summary> Метка последовательности </summary>
        public string SequenceLabel => sequenceLabel;

        /// <summary> Записи шагов последовательности </summary>
        public IReadOnlyList<StepEntry> Steps => steps;

        [SerializeField] private string sequenceLabel;
        [SerializeField] private StepEntry[] steps = Array.Empty<StepEntry>();

        /// <summary>
        /// Доступность группы (свойство группы, хранится на её записях; берётся с первой записи группы)
        /// </summary>
        /// <param name="groupIndex">Индекс группы</param>
        public GroupAvailability AvailabilityOf(int groupIndex)
        {
            foreach (var entry in steps)
                if (entry != null && entry.GroupIndex == groupIndex)
                    return entry.Availability;

            return GroupAvailability.AfterPreviousGroups;
        }

        /// <summary>
        /// Запись шага: ссылка на ассет-шаг, индекс параллельной группы и эффекты оркестрации
        /// </summary>
        [Serializable]
        public sealed class StepEntry
        {
            /// <summary> Шаг (ассет, реализующий <see cref="IPuzzleStep"/>) </summary>
            public IPuzzleStep Step => step as IPuzzleStep;

            /// <summary> Индекс параллельной группы </summary>
            public int GroupIndex => groupIndex;

            /// <summary> Доступность группы (свойство группы; на всех её записях должно совпадать) </summary>
            public GroupAvailability Availability => availability;

            /// <summary> Эффекты при активации шага </summary>
            public IReadOnlyList<PuzzleEffect> ActivationEffects => activationEffects;

            /// <summary> Эффекты при завершении шага </summary>
            public IReadOnlyList<PuzzleEffect> CompletionEffects => completionEffects;

            [SerializeField] private IdentifiableObject step;
            [Tooltip("Шаги с одинаковым GroupIndex активируются одновременно. " +
                     "Следующая группа стартует после завершения текущей")]
            [SerializeField] private int groupIndex;
            [Tooltip("Доступность группы: с самого старта (Always) или только после предыдущих групп")]
            [SerializeField] private GroupAvailability availability;
            [Tooltip("Выполняются при активации шага — до ожидания завершения")]
            [SerializeReference] private PuzzleEffect[] activationEffects = Array.Empty<PuzzleEffect>();
            [Tooltip("Выполняются при завершении шага")]
            [SerializeReference] private PuzzleEffect[] completionEffects = Array.Empty<PuzzleEffect>();
        }
    }
}
