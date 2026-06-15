using System;
using Extensions.Attributes;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Запись эффекта шага
    /// </summary>
    [Serializable]
    public sealed class EffectEntry
    {
        /// <summary> Событие шага, по которому выполняется эффект </summary>
        public TriggerKind Trigger => trigger;
        /// <summary> Выполняемый эффект </summary>
        public SequenceStepEffect Effect => effect;

        [Tooltip("Событие шага, по которому выполняется эффект (как у вьюшек)")]
        [EnumRange(0, 9)]
        [SerializeField] private TriggerKind trigger;
        [SerializeReference] private SequenceStepEffect effect;
    }
}
