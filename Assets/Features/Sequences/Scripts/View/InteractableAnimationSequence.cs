using Extensions.AnimationSequencer;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Проигрывание AnimationSequencer на события шага (прямое — Command, откат — ReverseMode)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableAnimationSequence : InteractableActionsBehaviour<AnimationSequenceAction>
    {
        protected override void Apply(AnimationSequenceAction action, bool reverse, bool silent)
        {
            var sequencer = action.Target;
            if (sequencer == null) return;

            if (reverse)
            {
                switch (action.ReverseMode)
                {
                    case AnimationReverseMode.Backwards: Backwards(sequencer, silent); break;
                    case AnimationReverseMode.Stop: StopAt(sequencer, silent); break;
                    default:
                        ServiceDebug.LogError(this, $"Необработанный {nameof(AnimationReverseMode)}: {action.ReverseMode}");
                        break;
                }
            }
            else
            {
                switch (action.Command)
                {
                    case AnimationCommand.Play: PlayForward(sequencer, silent); break;
                    case AnimationCommand.Stop: StopAt(sequencer, silent); break;
                    default:
                        ServiceDebug.LogError(this, $"Необработанный {nameof(AnimationCommand)}: {action.Command}");
                        break;
                }
            }
        }

        private static void PlayForward(AnimationSequencer sequencer, bool silent)
        {
            if (silent) sequencer.SetAtEnd();
            else sequencer.Play();
        }

        private static void Backwards(AnimationSequencer sequencer, bool silent)
        {
            if (silent) sequencer.SetAtStart();
            else sequencer.PlayBackwards();
        }

        private static void StopAt(AnimationSequencer sequencer, bool silent) => sequencer.SetAtStart();
    }
}
