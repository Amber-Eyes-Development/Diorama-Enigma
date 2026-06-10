using Extensions.Audio;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// 3D one-shot звуки на события шага (через <see cref="AudioController"/>)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableSounds : InteractableActionsBehaviour<SoundAction>
    {
        protected override void Apply(SoundAction action, bool reverse, bool silent)
        {
            // Тихое восстановление звук не проигрывает (откат на reversible отсекает база)
            if (silent || action.Sound == null) return;

            AudioController controller = AudioController.Instance;
            if (controller == null) return;

            controller.Play(action.Sound, transform.position, action.Model);
        }
    }
}
