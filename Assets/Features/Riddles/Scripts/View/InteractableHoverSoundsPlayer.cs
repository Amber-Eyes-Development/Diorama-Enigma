using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Звук при наведении и уходе курсора
    /// </summary>
    public sealed class InteractableHoverSoundsPlayer : InteractableAudioBehaviour
    {
        [Header("Звуки hover"), Space]
        [SerializeField] private AudioResource enterSound;
        [SerializeField] private AudioResource exitSound;

        protected override void Subscribe() => Interactable.onHoverChanged += OnHoverChanged;

        protected override void Unsubscribe() => Interactable.onHoverChanged -= OnHoverChanged;

        private void OnHoverChanged(bool isHovered)
        {
            AudioResource sound = isHovered ? enterSound : exitSound;

            if (sound != null) Play(sound);
        }
    }
}
