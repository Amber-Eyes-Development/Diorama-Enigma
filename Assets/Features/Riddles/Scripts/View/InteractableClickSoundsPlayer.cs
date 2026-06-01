using System;
using Extensions.Audio;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Воспроизводит звук при смене стейта <see cref="InteractableObject"/>.
    /// Каждому стейту назначается свой <see cref="AudioResource"/>.
    /// Размещается на том же GameObject.
    /// </summary>
    public sealed class InteractableClickSoundsPlayer : BaseAudioPlayer
    {
        /// <summary>Звук для одного стейта</summary>
        [Serializable]
        public struct StateSound
        {
            [Tooltip("Индекс стейта из InteractableObject")]
            public int StateIndex;

            [Tooltip("Звук, воспроизводимый при переходе в этот стейт")]
            public AudioResource Sound;
        }

        [Header("Звуки по стейтам"), Space]
        [SerializeField] private StateSound[] stateSounds;

        private InteractableObject interactable;

        #region Unity Lifecycle

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

        #region Internal

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

        #endregion
    }
}
