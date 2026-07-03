using System.Collections.Generic;

namespace Extensions.Sequences
{
    /// <summary>
    /// Последовательность: упорядоченные группы записей шагов. POCO, единая для авторского и процедурного контента.
    /// </summary>
    public sealed class Sequence
    {
        /// <summary> Идентификатор (ключ сейва снапшота) </summary>
        public string Id => id;
        /// <summary> Записи шагов </summary>
        public IReadOnlyList<StepEntry> Steps => steps;

        private readonly string id;
        private readonly List<StepEntry> steps;
        private Dictionary<string, Step> byId;

        /// <summary> Новая последовательность </summary>
        public Sequence(string id, IReadOnlyList<StepEntry> steps)
        {
            this.id = id;
            this.steps = steps != null ? new List<StepEntry>(steps) : new List<StepEntry>();
        }

        /// <summary> Завершена ли последовательность (есть шаги и все завершены) </summary>
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

        /// <summary> Добавить запись на лету (для процедурной мутации; после — вызвать <see cref="SequenceEngine.Refresh"/>) </summary>
        public void Add(StepEntry entry)
        {
            if (entry == null) return;
            steps.Add(entry);
            byId = null;
        }

        /// <summary> Найти шаг записи по id </summary>
        public Step Step(string stepId)
        {
            EnsureIndex();
            return byId.TryGetValue(stepId, out var step) ? step : null;
        }

        /// <summary> Найти шаг записи по id с приведением типа </summary>
        public T Step<T>(string stepId) where T : Step => Step(stepId) as T;

        /// <summary> Доступность группы (берётся с первой записи группы) </summary>
        public GroupAvailability AvailabilityOf(int groupIndex)
        {
            foreach (var entry in steps)
                if (entry != null && entry.GroupIndex == groupIndex)
                    return entry.Availability;

            return GroupAvailability.AfterPreviousGroups;
        }

        /// <summary> Остаётся ли группа интерактивной после завершения (берётся с первой записи группы) </summary>
        public bool InteractableAfterCompletionOf(int groupIndex)
        {
            foreach (var entry in steps)
                if (entry != null && entry.GroupIndex == groupIndex)
                    return entry.InteractableAfterCompletion;

            return false;
        }

        private void EnsureIndex()
        {
            if (byId != null) return;

            byId = new Dictionary<string, Step>();
            foreach (var entry in steps)
            {
                var step = entry?.Step;
                if (step == null || string.IsNullOrEmpty(step.Id)) continue;
                byId[step.Id] = step;
            }
        }
    }
}
