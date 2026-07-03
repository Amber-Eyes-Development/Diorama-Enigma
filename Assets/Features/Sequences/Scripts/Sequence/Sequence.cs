using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Последовательность шагов <see cref="SequenceStep"/>: упорядоченные группы ссылок на ассеты-шаги
    /// </summary>
    [CreateAssetMenu(menuName = "StepSequences/Sequence", fileName = nameof(Sequence))]
    public sealed class Sequence : ScriptableObject
    {
        /// <summary> Записи шагов последовательности </summary>
        public IReadOnlyList<StepEntry> Steps => steps;

        /// <summary>
        /// Завершена ли последовательность целиком (ВСЕ шаги, включая Always-группы)
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

        /// <summary>
        /// Пройдены ли все линейные группы (Always-группы не учитываются)
        /// </summary>
        /// <remarks>
        /// Совпадает с моментом <see cref="SequenceRunner.onSequenceCompleted"/>: раннер завершается по
        /// линейным группам, а фоновые (Always) шаги в порядок прохождения не входят. false, если линейных групп нет
        /// </remarks>
        public bool IsSolved
        {
            get
            {
                bool any = false;
                foreach (var entry in steps)
                {
                    if (entry?.Step == null || entry.Availability == GroupAvailability.Always) continue;
                    any = true;
                    if (!entry.Step.IsCompleted) return false;
                }

                return any;
            }
        }

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
