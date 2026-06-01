using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;

namespace Extensions.Audio
{
    /// <summary>
    /// Базовый плеер аудио
    /// </summary>
    public abstract class BaseAudioPlayer : MonoBehaviour
    {
        [Header("Воспроизведение"), Space]

        [Tooltip("Тип аудио трека")]
        [SerializeField] protected AudioModel model = AudioModel.Sfx;
        [Tooltip("Оставить пустым, если нужны дефолтные значения от типа аудио")]
        [SerializeField] protected AudioSpatialPreset spatialPreset;
        
        protected AudioController audioController;
        protected AudioDefaults defaults;

        protected virtual void OnEnable()
        {
            audioController = AudioController.Instance;
            audioController.onVolumeChanged += OnVolumeSettingsChanged;

            if (audioController == null)
            {
                ServiceDebug.LogError("Контроллер аудио не найден, звуки воспроизводиться не будут");
                return;
            }

            defaults = audioController.GetDefaults(model);

            if (defaults.volume != null)
            {
                defaults.volume.onValueChanged += OnVolumeChanged;
                OnVolumeChanged(defaults.volume.Value);
            }
            else
            {
                OnVolumeChanged(1f);
            }
        }

        protected virtual void OnDisable()
        {
            if (defaults.volume != null)
                defaults.volume.onValueChanged -= OnVolumeChanged;
            
            if (audioController != null)
                audioController.onVolumeChanged -= OnVolumeSettingsChanged;
        }

        protected virtual void Play(AudioResource audioResource)
        {
            if (audioController == null) return;
            
            AudioSpatialPreset currentSpatialPreset = spatialPreset;
            if (currentSpatialPreset == null)
            {
                currentSpatialPreset = defaults.spatialPreset;
            }

            if (currentSpatialPreset != null && currentSpatialPreset.SpatialBlend != 0)
            {
                audioController.Play(audioResource, this.transform.position, model, currentSpatialPreset);
                return;
            }

            audioController.Play(audioResource, model, currentSpatialPreset);
        }

        protected virtual float GetAppliedVolume(float volumeModifier)
        {
            if (audioController == null)
            {
                return volumeModifier;
            }

            return audioController.GetAppliedVolume(model, volumeModifier);
        }
        protected virtual void OnVolumeChanged(float volume) { }
        
        protected virtual void OnVolumeSettingsChanged() { }
    }
}