using System;
using DioramaEnigma.Sequences;
using Extensions.Attributes;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Данные входящей связи диорамы
    /// </summary>
    [Serializable]
    public sealed class DioramaLink
    {
        /// <summary> Диорама-источник связи </summary>
        public DioramaDefinition Source => source;
        /// <summary> Условие открытия по этой связи </summary>
        public DioramaLinkCondition Condition => condition;
        /// <summary> Шаг-условие (только для <see cref="DioramaLinkCondition.StepTrigger"/>) </summary>
        public SequenceStep Step => step;
        /// <summary> Состояние-триггер шага, при котором связь открывает диораму </summary>
        public TriggerKind StepTrigger => stepTrigger;

        [Tooltip("Диорама-источник: связь ведёт от неё к текущей")]
        [SerializeField] private DioramaDefinition source;
        [Tooltip("Условие открытия текущей диорамы по этой связи")]
        [SerializeField] private DioramaLinkCondition condition;
        [Tooltip("Шаг, состояние которого открывает диораму. " +
                 "Должен быть SequenceStep — композит офлайн не оповещает сервис доступа")]
        [SerializeField] private SequenceStep step;
        [Tooltip("Состояние шага, по которому открывается диорама")]
        [EnumRange(0, 9)]
        [SerializeField] private TriggerKind stepTrigger = TriggerKind.Completed;
    }
}
