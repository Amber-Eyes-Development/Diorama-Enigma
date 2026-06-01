using Extensions.Audio;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Воспроизводит звуки при наведении и уходе курсора с <see cref="InteractableObject"/>.
    /// Размещается на том же GameObject. Наследует <see cref="BaseAudioPlayer"/> —
    /// звук учитывает аудио-модель, пространственный пресет и настройку громкости.
    /// </summary>
    public sealed class InteractableHoverSoundsPlayer : BaseAudioPlayer
    {
        [Header("Звуки hover"), Space]
        [SerializeField] private AudioResource enterSound;
        [SerializeField] private AudioResource exitSound;

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
                interactable.onHoverChanged += OnHoverChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (interactable != null)
                interactable.onHoverChanged -= OnHoverChanged;
        }

        #endregion

        #region Internal

        private void OnHoverChanged(bool isHovered)
        {
            AudioResource sound = isHovered ? enterSound : exitSound;

            if (sound != null) Play(sound);
        }

        #endregion
    }
}
