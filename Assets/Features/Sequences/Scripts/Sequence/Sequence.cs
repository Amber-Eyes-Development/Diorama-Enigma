using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Последовательность шагов <see cref="SequenceStep"/>: упорядоченные группы ссылок на ассеты-шаги
    /// </summary>
    [CreateAssetMenu(menuName = "Sequences/Sequence", fileName = nameof(Sequence))]
    public sealed class Sequence : ScriptableObject
    {
        /// <summary> Метка последовательности </summary>
        public string SequenceLabel => sequenceLabel;
        /// <summary> Записи шагов последовательности </summary>
        public IReadOnlyList<StepEntry> Steps => steps;

        /// <summary>
        /// Завершена ли последовательность
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                bool any = false;
                foreach (var entry in steps)
                {
                    if (entry?.Step == null) continue;
                    any = true;
                    if (!entry.Step.IsCompleted) return false;
                }

                return any;
            }
        }

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
        /// Остаётся ли группа интерактивной после завершения (берётся с первой записи группы)
        /// </summary>
        /// <param name="groupIndex">Индекс группы</param>
        public bool InteractableAfterCompletionOf(int groupIndex)
        {
            foreach (var entry in steps)
                if (entry != null && entry.GroupIndex == groupIndex)
                    return entry.InteractableAfterCompletion;

            return false;
        }
    }
}
