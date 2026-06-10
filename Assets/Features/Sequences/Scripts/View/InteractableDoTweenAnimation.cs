using DG.Tweening;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Проигрывание DOTweenAnimation на события шага (прямое — Command, откат — ReverseMode)
    /// </summary>
    // Раньше DOTweenAnimation (порядок по умолчанию 0): успеть снять autoPlay до того, как тот создаст твин в своём Awake
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class InteractableDoTweenAnimation : InteractableActionsBehaviour<DoTweenAnimationAction>
    {
        protected override void Apply(DoTweenAnimationAction action, bool reverse, bool silent)
        {
            var animation = action.Target;
            if (animation == null) return;

            if (reverse)
            {
                if (action.ReverseMode == AnimationReverseMode.Backwards) Backwards(animation, silent);
                else StopAt(animation, silent);
            }
            else
            {
                if (action.Command == AnimationCommand.Play) PlayForward(animation, silent);
                else StopAt(animation, silent);
            }
        }

        private static void PlayForward(DOTweenAnimation animation, bool silent)
        {
            animation.DORestart();
            if (silent) animation.DOComplete();
        }

        private static void Backwards(DOTweenAnimation animation, bool silent)
        {
            if (silent)
            {
                animation.DORewind();
                return;
            }

            animation.DOComplete();
            animation.DOPlayBackwards();
        }

        private static void StopAt(DOTweenAnimation animation, bool silent) => animation.DORewind();
    }
}
