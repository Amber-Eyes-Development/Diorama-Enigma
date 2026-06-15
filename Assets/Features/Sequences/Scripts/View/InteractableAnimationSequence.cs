using Extensions.AnimationSequencer;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Проигрывание AnimationSequencer на события шага
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableAnimationSequence : InteractableActionsBehaviour<AnimationSequenceAction>
    {
        protected override void Apply(AnimationSequenceAction action, bool reverse, bool silent)
        {
            var sequencer = action.Target;
            if (sequencer == null) return;

            AnimationCommand command = reverse ? action.ReverseCommand : action.Command;
            switch (command)
            {
                case AnimationCommand.Play: PlayForward(sequencer, silent); break;
                case AnimationCommand.PlayBackwards: Backwards(sequencer, silent); break;
                case AnimationCommand.Stop: StopAt(sequencer, silent); break;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(AnimationCommand)}: {command}");
                    break;
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
