using DG.Tweening;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Проигрывание DOTweenAnimation на события шага
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class InteractableDoTweenAnimation : InteractableActionsBehaviour<DoTweenAnimationAction>
    {
        // Заведомо больше длительности любого твина → позиция «конец»
        private const float END_POSITION = 99999f;

        protected override void Apply(DoTweenAnimationAction action, bool reverse, bool silent)
        {
            var animation = action.Target;
            if (animation == null) return;

            EnsureTween(animation);

            AnimationCommand command = reverse ? action.ReverseCommand : action.Command;
            switch (command)
            {
                case AnimationCommand.Play: PlayForward(animation, silent); break;
                case AnimationCommand.PlayBackwards: Backwards(animation, silent); break;
                case AnimationCommand.Stop: StopAt(animation, silent); break;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(AnimationCommand)}: {command}");
                    break;
            }
        }

        private static void PlayForward(DOTweenAnimation animation, bool silent)
        {
            if (silent)
            {
                animation.DOGotoAndPause(END_POSITION);
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

        /// <summary> Создать твин, если его ещё нет (вью владеет жизненным циклом — autoGenerate отключён) </summary>
        private static void EnsureTween(DOTweenAnimation animation)
        {
            if (animation.tween == null) animation.CreateTween(regenerateIfExists: false, andPlay: false);
        }
    }
}
