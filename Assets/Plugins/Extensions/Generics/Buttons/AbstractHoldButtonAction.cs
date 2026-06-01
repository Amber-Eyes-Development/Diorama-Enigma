using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция действия по зажатию кнопки <see cref="Button"/>
    /// </summary>
    [RequireComponent(typeof(HoldButtonClickOrchestrator))]
    public abstract class AbstractHoldButtonAction : BaseAbstractButtonAction
    {
        protected HoldButtonClickOrchestrator orchestrator;

        protected virtual void Awake()
        {
            orchestrator = GetComponent<HoldButtonClickOrchestrator>();
            if (orchestrator == null) orchestrator = gameObject.AddComponent<HoldButtonClickOrchestrator>();
        }

        protected virtual void OnEnable() => orchestrator.AddAction(this);

        protected virtual void OnDisable() => orchestrator?.RemoveAction(this);
    }
}
