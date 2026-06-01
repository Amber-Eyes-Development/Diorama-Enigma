using System;
using UnityEngine;
using UnityEngine.Events;

namespace Extensions.Helpers.LifecycleActions
{
    /// <summary>
    /// Действие жизненного цикла <see cref="MonoBehaviour"/>
    /// </summary>
    [Serializable]
    public sealed class LifecycleAction
    {
        [SerializeField] private LifecycleMoment moment;
        [SerializeField] private UnityEvent action;

        public LifecycleMoment Moment => moment;

        public void Invoke()
        {
            if (action == null) return;
            action.Invoke();
        }
    }
}