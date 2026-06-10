using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Вызов кастомных эффектов (<see cref="AbstractCustomView"/>) на события шага
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableCustomView : InteractableActionsBehaviour<CustomViewAction>
    {
        protected override void Apply(CustomViewAction action, bool reverse, bool silent) =>
            action.Target?.Apply(reverse, silent);
    }
}
