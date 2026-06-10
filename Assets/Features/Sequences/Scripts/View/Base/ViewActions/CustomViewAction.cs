using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: вызов кастомного эффекта <see cref="AbstractCustomView"/> на событие шага
    /// </summary>
    [Serializable]
    public sealed class CustomViewAction : ViewAction
    {
        /// <summary> Целевой кастомный эффект </summary>
        public AbstractCustomView Target => target;

        public override void EnsureTarget(GameObject host)
        {
            if (target == null)
                target = host.GetComponent<AbstractCustomView>();
        }

        public override void PrepareTarget() => target?.Prepare();

        [Tooltip("Целевой кастомный эффект (наследник AbstractCustomView)")]
        [SerializeField] private AbstractCustomView target;
    }
}
