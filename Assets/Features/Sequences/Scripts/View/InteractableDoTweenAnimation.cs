using DG.Tweening;
using Extensions.Log;
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
        // Заведомо больше длительности любого твина → позиция «конец»
        private const float EndPosition = 99999f;

        protected override void Apply(DoTweenAnimationAction action, bool reverse, bool silent)
        {
            var animation = action.Target;
            if (animation == null) return;

            if (reverse)
            {
                switch (action.ReverseMode)
                {
                    case AnimationReverseMode.Backwards: Backwards(animation, silent); break;
                    case AnimationReverseMode.Stop: StopAt(animation, silent); break;
                    default:
                        ServiceDebug.LogError(this, $"Необработанный {nameof(AnimationReverseMode)}: {action.ReverseMode}");
                        break;
                }
            }
            else
            {
                switch (action.Command)
                {
                    case AnimationCommand.Play: PlayForward(animation, silent); break;
                    case AnimationCommand.Stop: StopAt(animation, silent); break;
                    default:
                        ServiceDebug.LogError(this, $"Необработанный {nameof(AnimationCommand)}: {action.Command}");
                        break;
                }
            }
        }

        private static void PlayForward(DOTweenAnimation animation, bool silent)
        {
            if (silent)
            {
                animation.DOGotoAndPause(EndPosition);
                return;
            }

            animation.DORestart();
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
