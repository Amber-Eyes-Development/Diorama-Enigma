using System.Collections.Generic;
using UnityEngine;

namespace Extensions.Helpers.LifecycleActions
{
    /// <summary>
    /// Вызов действий жизненного цикла <see cref="MonoBehaviour"/>
    /// </summary>
    public sealed class LifecycleActionInvoker : MonoBehaviour
    {
        [SerializeField] private List<LifecycleAction> actions = new();

        private void Awake() => InvokeActions(LifecycleMoment.Awake);

        private void OnEnable() => InvokeActions(LifecycleMoment.OnEnable);

        private void Start() => InvokeActions(LifecycleMoment.Start);

        private void OnDisable() => InvokeActions(LifecycleMoment.OnDisable);

        private void OnDestroy() => InvokeActions(LifecycleMoment.OnDestroy);

        private void InvokeActions(LifecycleMoment moment)
        {
            foreach (var action in actions)
            {
                if (action.Moment != moment) continue;
                action.Invoke();
            }
        }
    }
}