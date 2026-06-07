using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary> Реакция системами частиц на событие шага (один биндинг; нужно ещё — добавь второй компонент) </summary>
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

        [SerializeField] private Binding binding;

        private Binding[] single;
        protected override IReadOnlyList<Binding> Bindings => single;

        protected override void Awake()
        {
            base.Awake();
            single = new[] { binding };
        }

        protected override void Apply(Binding binding, bool silent) => binding.Actions.Apply(silent);
    }
}
