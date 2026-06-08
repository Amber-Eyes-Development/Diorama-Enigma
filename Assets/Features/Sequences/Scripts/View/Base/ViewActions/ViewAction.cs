using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Базовая запись действия вьюшки: триггер (событие шага) и обратимость
    /// </summary>
    [Serializable]
    public abstract class ViewAction
    {
        /// <summary> Событие шага, на которое срабатывает действие </summary>
        public TriggerKind Trigger => trigger;
        /// <summary> Откатывать действие при переходе шага в противоположное триггеру состояние </summary>
        public bool Reversible => reversible;

        [Tooltip("Событие шага, на которое срабатывает действие")]
        [SerializeField] private TriggerKind trigger;
        [Tooltip("Откатывать действие при переходе шага в противоположное триггеру состояние")]
        [SerializeField] private bool reversible = true;
    }
}
