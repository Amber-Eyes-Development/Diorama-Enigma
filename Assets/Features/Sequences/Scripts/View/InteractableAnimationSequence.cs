using Extensions.AnimationSequencer;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Реакция на событие шага запуском AnimationSequencer
    /// </summary>
    public sealed class InteractableAnimationSequence : InteractableReactionBehaviour
    {
        [SerializeField] private AnimationSequencer sequencer;

        protected override void React(bool silent)
        {
            if (!silent && sequencer != null)
                sequencer.Play();
        }
    }
}
