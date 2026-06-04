using System;
using DG.Tweening;
using Extensions.AnimationSequencer;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Визуальная реакция (объекты, анимации) на события объекта
    /// </summary>
    public sealed class InteractableVisuals : InteractableReactionBehaviour<InteractableVisuals.Binding>
    {
        /// <summary> Объекты и анимации для одного условия </summary>
        [Serializable]
        public struct Binding : IReactionBinding
        {
            public GameObjectActivation[] Objects;
            public DOTweenAnimation Animation;
            public AnimationSequencer Sequencer;
            public readonly ReactionTrigger ReactionTrigger => reactionTrigger;
            
            [SerializeField] private ReactionTrigger reactionTrigger;
        }

        protected override void Apply(Binding binding, bool silent)
        {
            binding.Objects.Apply();

            if (binding.Animation != null)
            {
                binding.Animation.DORestart();
                if (silent) binding.Animation.DOComplete();
            }

            if (!silent && binding.Sequencer != null)
                binding.Sequencer.Play();
        }
    }
}
