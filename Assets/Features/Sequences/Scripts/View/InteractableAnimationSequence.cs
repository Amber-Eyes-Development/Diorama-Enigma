using Extensions.AnimationSequencer;
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
                if (action.ReverseMode == AnimationReverseMode.Backwards) Backwards(sequencer, silent);
                else StopAt(sequencer, silent);
            }
            else
            {
                if (action.Command == AnimationCommand.Play) PlayForward(sequencer, silent);
                else StopAt(sequencer, silent);
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
