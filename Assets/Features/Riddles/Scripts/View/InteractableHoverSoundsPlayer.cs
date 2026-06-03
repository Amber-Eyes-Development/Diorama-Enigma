using Extensions.Audio;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Звук при наведении и уходе курсора
    /// </summary>
    public sealed class InteractableHoverSoundsPlayer : BaseAudioPlayer
    {
        [Header("Звуки hover"), Space]
        [SerializeField] private AudioResource enterSound;
        [SerializeField] private AudioResource exitSound;

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
                interactable.onHoverChanged += OnHoverChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (interactable != null)
                interactable.onHoverChanged -= OnHoverChanged;
        }

        #endregion

        private void OnHoverChanged(bool isHovered)
        {
            AudioResource sound = isHovered ? enterSound : exitSound;

            if (sound != null) Play(sound);
        }
    }
}
