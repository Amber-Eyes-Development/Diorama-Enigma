using Extensions.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Звуковая реакция на событие шага (3D one-shot через <see cref="AudioController"/>)
    /// </summary>
    public sealed class InteractableSounds : InteractableReactionBehaviour
    {
        [SerializeField] private AudioResource sound;
        [Tooltip("Тип аудио трека")]
        [SerializeField] private AudioModel model = AudioModel.Sfx;

        protected override void React(bool silent)
        {
            if (silent || sound == null) return;

            AudioController controller = AudioController.Instance;
            if (controller == null) return;

            controller.Play(sound, transform.position, model);
        }
    }
}
