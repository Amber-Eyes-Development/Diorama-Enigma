using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Переключение активности объектов на события шага
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableObjectsSwitcher : InteractableActionsBehaviour<GameObjectActivationAction>
    {
        protected override void Apply(GameObjectActivationAction action, bool reverse, bool silent)
        {
            if (action.Target == null) return;

            action.Target.SetActive(reverse ? !action.Active : action.Active);
        }
    }
}
