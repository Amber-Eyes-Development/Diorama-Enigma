using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция действия по нажатию на кнопку <see cref="Button"/>
    /// </summary>
    [RequireComponent(typeof(ButtonClickOrchestrator))]
    public abstract class AbstractButtonAction : BaseAbstractButtonAction
    {
        protected ButtonClickOrchestrator orchestrator;

        protected virtual void Awake()
        {
            orchestrator = GetComponent<ButtonClickOrchestrator>();
            if (orchestrator == null) orchestrator = gameObject.AddComponent<ButtonClickOrchestrator>();
        }

        protected virtual void OnEnable() => orchestrator.AddAction(this);

        protected virtual void OnDisable() => orchestrator?.RemoveAction(this);
    }
}
