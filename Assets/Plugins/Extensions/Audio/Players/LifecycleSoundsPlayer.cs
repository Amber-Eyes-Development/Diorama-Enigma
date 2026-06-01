using System;
using System.Collections.Generic;
using Extensions.Helpers.LifecycleActions;
using UnityEngine;
using UnityEngine.Audio;

namespace Extensions.Audio
{
    /// <summary>
    /// Воспроизведение звуков при событиях жизненного цикла
    /// </summary>
    public sealed class LifecycleSoundsPlayer : BaseAudioPlayer
    {
        [Header("Звуки"), Space]
        [SerializeField] private List<LifecycleSound> sounds = new();

        private void Awake() => PlaySounds(LifecycleMoment.Awake);

        protected override void OnEnable()
        {
            base.OnEnable();
            PlaySounds(LifecycleMoment.OnEnable);
        }

        private void Start() => PlaySounds(LifecycleMoment.Start);

        protected override void OnDisable()
        {
            PlaySounds(LifecycleMoment.OnDisable);
            base.OnDisable();
        }

        private void OnDestroy() => PlaySounds(LifecycleMoment.OnDestroy);

        private void PlaySounds(LifecycleMoment moment)
        {
            foreach (var entry in sounds)
            {
                if (entry.Moment != moment) continue;
                if (entry.Sound == null) continue;
                Play(entry.Sound);
            }
        }
    }
    
    [Serializable]
    public sealed class LifecycleSound
    {
        [SerializeField] private LifecycleMoment moment;
        [SerializeField] private AudioResource sound;

        public LifecycleMoment Moment => moment;
        public AudioResource Sound => sound;
    }
}
