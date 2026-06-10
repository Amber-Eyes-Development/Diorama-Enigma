using System;
using Extensions.Attributes;
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
        /// <summary> Задержка применения действия от срабатывания триггера, сек </summary>
        public float Delay => delay;
        /// <summary> Откатывать действие при переходе шага в противоположное триггеру состояние </summary>
        public bool Reversible => reversible;

        /// <summary> При необходимости создать на хосте вьюшки целевой компонент и привязать его </summary>
        public virtual void EnsureTarget(GameObject host) { }

        /// <summary> Рантайм: подготовить целевой компонент к управлению вьюшкой (напр. снять авто-старт) </summary>
        public virtual void PrepareTarget() { }

        [Tooltip("Событие шага, на которое срабатывает действие")]
        [SerializeField] private TriggerKind trigger;
        [Tooltip("Задержка применения действия от срабатывания триггера, сек")]
        [SoftRange(0f, 3f, 1)]
        [SerializeField] private float delay = 0f;
        [Tooltip("Откатывать действие при переходе шага в противоположное триггеру состояние")]
        [SerializeField] private bool reversible = true;
    }
}
