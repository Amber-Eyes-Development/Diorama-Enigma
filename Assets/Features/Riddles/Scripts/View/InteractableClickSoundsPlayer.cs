using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Звук при смене стейта объекта
    /// </summary>
    public sealed class InteractableClickSoundsPlayer : InteractableAudioBehaviour
    {
        /// <summary> Звук для одного стейта </summary>
        [Serializable]
        public struct StateSound
        {
            public int StateIndex;
            public AudioResource Sound;
        }

        [Header("Звуки по стейтам"), Space]
        [SerializeField] private StateSound[] stateSounds;

        protected override void Subscribe() => Interactable.State.onStateChanged += OnStateChanged;

        protected override void Unsubscribe() => Interactable.State.onStateChanged -= OnStateChanged;

        private void OnStateChanged(int stateIndex)
        {
            foreach (var stateSound in stateSounds)
            {
                if (stateSound.StateIndex != stateIndex) continue;
                if (stateSound.Sound == null) break;

                Play(stateSound.Sound);
                return;
            }
        }
    }
}
