using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Окрашивание uGUI-графики на события шага (напр. дверь карты: красная, пока гейт-шаг не выполнен)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableGraphicTint : InteractableActionsBehaviour<GraphicTintAction>
    {
        protected override bool RestoresRestingState => true;

        protected override void Apply(GraphicTintAction action, bool reverse, bool silent)
        {
            if (action.Target == null) return;

            action.Target.color = reverse ? action.RestColor : action.Color;
        }
    }
}
