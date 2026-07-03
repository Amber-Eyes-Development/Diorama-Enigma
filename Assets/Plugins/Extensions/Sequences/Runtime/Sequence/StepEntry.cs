using System;
using System.Collections.Generic;

namespace Extensions.Sequences
{
    /// <summary>
    /// Запись шага в последовательности: шаг + индекс группы + доступность + гейты + эффекты
    /// </summary>
    public sealed class StepEntry
    {
        /// <summary> Шаг </summary>
        public Step Step => step;
        /// <summary> Индекс параллельной группы </summary>
        public int GroupIndex => groupIndex;
        /// <summary> Доступность группы </summary>
        public GroupAvailability Availability => availability;
        /// <summary> Остаётся ли группа интерактивной после завершения </summary>
        public bool InteractableAfterCompletion => interactableAfterCompletion;
        /// <summary> Условия доступа (помимо порядка групп): все должны быть выполнены </summary>
        public IReadOnlyList<Gate> Gates => gates;
        /// <summary> Эффекты записи (каждый со своим триггером) </summary>
        public IReadOnlyList<EffectEntry> Effects => effects;

        private readonly Step step;
        private readonly int groupIndex;
        private readonly GroupAvailability availability;
        private readonly bool interactableAfterCompletion;
        private readonly IReadOnlyList<Gate> gates;
        private readonly IReadOnlyList<EffectEntry> effects;

        /// <summary> Новая запись шага </summary>
        public StepEntry(
            Step step,
            int groupIndex,
            GroupAvailability availability = GroupAvailability.AfterPreviousGroups,
            bool interactableAfterCompletion = false,
            IReadOnlyList<Gate> gates = null,
            IReadOnlyList<EffectEntry> effects = null)
        {
            this.step = step;
            this.groupIndex = groupIndex;
            this.availability = availability;
            this.interactableAfterCompletion = interactableAfterCompletion;
            this.gates = gates ?? Array.Empty<Gate>();
            this.effects = effects ?? Array.Empty<EffectEntry>();
        }
    }
}
