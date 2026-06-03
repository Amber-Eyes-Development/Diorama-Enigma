using System;
using Extensions.Audio;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Звук при смене состояния объекта
    /// </summary>
    public sealed class InteractableClickSoundsPlayer : BaseAudioPlayer
    {
        /// <summary> Звук для одного состояния </summary>
        [Serializable]
        public struct StateSound
        {
            public int StateIndex;
            public AudioResource Sound;
        }

        [Header("Звуки по состояниям"), Space]
        [SerializeField] private StateSound[] stateSounds;

        private InteractableObject interactable;

        #region MonoBehaviour

        private void Awake()
        {
            interactable = GetComponent<InteractableObject>();

            if (interactable == null)
                ServiceDebug.LogWarning(this, "InteractableObject не найден на этом объекте");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (interactable != null)
                interactable.State.onStateChanged += OnStateChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (interactable != null)
                interactable.State.onStateChanged -= OnStateChanged;
        }

        #endregion

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
