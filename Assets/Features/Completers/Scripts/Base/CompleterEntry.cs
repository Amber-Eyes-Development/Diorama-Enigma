using System;
using DioramaEnigma.Sequences;
using Extensions.Attributes;
using UnityEngine;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Запись наблюдаемого шага: шаг + условие, при котором он считается истинным операндом
    /// </summary>
    [Serializable]
    public struct CompleterEntry
    {
        /// <summary> Наблюдаемый шаг </summary>
        public AbstractSequenceStep Step => step;
        /// <summary> Состояние шага, при котором запись считается истинной (как у вьюшек/гейтов) </summary>
        public TriggerKind Trigger => trigger;

        [SerializeField] private AbstractSequenceStep step;
        [Tooltip("Состояние шага, при котором запись считается истинной")]
        [EnumRange(0, 9)]
        [SerializeField] private TriggerKind trigger;

        /// <summary> Выполнено ли условие записи прямо сейчас (запись без шага — всегда false) </summary>
        public bool IsSatisfied => step != null && trigger.IsSatisfiedBy(step.IsCompleted, step.IsUnlocked);
    }
}
