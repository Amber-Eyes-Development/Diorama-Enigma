using System;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary> Реакция системами частиц на события объекта </summary>
    public sealed class InteractableParticles : InteractableReactionBehaviour<InteractableParticles.Binding>
    {
        /// <summary> Действия с частицами для одного условия </summary>
        [Serializable]
        public struct Binding : IReactionBinding
        {
            public ParticleSystemAction[] Actions;
            public readonly ReactionTrigger ReactionTrigger => reactionTrigger;
            
            [SerializeField] private ReactionTrigger reactionTrigger;
        }

        protected override void Apply(Binding binding, bool silent) => binding.Actions.Apply(silent);
    }
}
