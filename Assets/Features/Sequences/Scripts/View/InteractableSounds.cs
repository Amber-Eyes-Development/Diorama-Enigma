using System.Collections.Generic;
using Extensions.Audio;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// 3D звуки (one-shot или зацикленные) на события шага (через <see cref="AudioController"/>)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableSounds : InteractableActionsBehaviour<SoundAction>
    {
        /// <summary> Активные зацикленные источники по действию — чтобы оборвать их на откате/повторе </summary>
        private Dictionary<SoundAction, AudioSource> loops;

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllLoops();
        }

        protected override void Apply(SoundAction action, bool reverse, bool silent)
        {
            if (reverse)
            {
                StopLoop(action);
                return;
            }

            if (action.Sound == null) return;
            // зацикленный звук — состояние, а не разовое событие: его поднимаем и при тихом восстановлении
            if (silent && !action.Loop) return;

            AudioController controller = AudioController.Instance;
            if (controller == null) return;

            StopLoop(action);
            AudioSource source = controller.Play(action.Sound, transform.position, action.Model, loop: action.Loop);

            if (action.Loop && source != null)
                (loops ??= new Dictionary<SoundAction, AudioSource>())[action] = source;
        }

        private void StopLoop(SoundAction action)
        {
            if (loops == null || !loops.TryGetValue(action, out AudioSource source)) return;

            loops.Remove(action);

            AudioController controller = AudioController.Instance;
            if (controller != null) controller.Stop(source);
        }

        private void StopAllLoops()
        {
            if (loops == null) return;

            AudioController controller = AudioController.Instance;
            if (controller != null)
                foreach (AudioSource source in loops.Values)
                    controller.Stop(source);

            loops.Clear();
        }
    }
}
