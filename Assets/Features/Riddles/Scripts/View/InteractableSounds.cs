using System;
using System.Collections.Generic;
using Extensions.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Звуковая реакция на события объекта (3D one-shot через <see cref="AudioController"/>)
    /// </summary>
    public sealed class InteractableSounds : InteractableReactionBehaviour<InteractableSounds.Binding>
    {
        /// <summary> Звук для одного условия </summary>
        [Serializable]
        public struct Binding : IReactionBinding
        {
            public AudioResource Sound;
            public readonly ReactionTrigger ReactionTrigger => reactionTrigger;
            
            [SerializeField] private ReactionTrigger reactionTrigger;
        }

        [SerializeField] private Binding[] bindings;
        protected override IReadOnlyList<Binding> Bindings => bindings;

        [Header("Воспроизведение"), Space]
        [Tooltip("Тип аудио трека")]
        [SerializeField] private AudioModel model = AudioModel.Sfx;

        protected override void Apply(Binding binding, bool silent)
        {
            // На восстановлении состояния звук не играем
            if (silent || binding.Sound == null) return;

            AudioController controller = AudioController.Instance;
            if (controller == null) return;

            controller.Play(binding.Sound, transform.position, model);
        }
    }
}
