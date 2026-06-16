using System;
using Extensions.Attributes;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Запись композитного шага: дочерний шаг + условие, при котором он засчитывается
    /// </summary>
    [Serializable]
    public struct CompositeStepEntry
    {
        /// <summary> Дочерний шаг </summary>
        public AbstractSequenceStep Step => step;
        /// <summary> Состояние шага, при котором запись засчитывается (как у вьюшек/гейтов) </summary>
        public TriggerKind Trigger => trigger;

        [SerializeField] private AbstractSequenceStep step;
        [Tooltip("Состояние шага, при котором запись засчитывается")]
        [EnumRange(0, 9)]
        [SerializeField] private TriggerKind trigger;

        /// <summary> Выполнено ли условие записи прямо сейчас (запись без шага — всегда false) </summary>
        public bool IsSatisfied => step != null && trigger.IsSatisfiedBy(step.IsCompleted, step.IsUnlocked);
    }
}
