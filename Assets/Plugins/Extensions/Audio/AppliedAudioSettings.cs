using UnityEngine.Audio;

namespace Extensions.Audio
{
    /// <summary>
    /// Структура для передачи настроек аудио
    /// </summary>
    public struct AppliedAudioSettings
    {
        public AudioMixerGroup mixerGroup;
        public float volume;
        public float pitch;
        public float spatialBlend;
        public float minDistance;
        public float maxDistance;
        public int priority;
        public bool loop;
    }
}