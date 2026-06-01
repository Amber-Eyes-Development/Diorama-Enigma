using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using Extensions.Coroutines;
using Extensions.Log;
using Extensions.Pool;
using Extensions.Singleton;
using Extensions.Helpers;
using Extensions.ScriptableValues;
using Random = UnityEngine.Random;

namespace Extensions.Audio
{
    
    /// <summary>
    /// Контроллер аудио
    /// </summary>
    /// <remarks>
    /// Воспроизводит звуки, музыку и эмбиенс программы
    /// </remarks>
    public sealed class AudioController : MonoBehaviourSingleton<AudioController>
    {
        private const int PREWARM_SOURCES_NUMBER = 4;
        
        /// <summary>
        /// Событие изменения громкости
        /// </summary>
        public event Action onVolumeChanged;
        
        [Header("Значения по-умолчанию")]
        [Space]
        
        [SerializeField]
        private FloatValue commonVolume;
        [SerializeField]
        private AudioDefaults musicDefaults;
        [SerializeField] 
        private AudioDefaults ambienceDefaults;
        [SerializeField] 
        private AudioDefaults uiDefaults;
        [SerializeField] 
        private AudioDefaults sfxDefaults;

        [Header("Фабрика источников аудио")]
        [Space]

        [SerializeField]
        private AudioSource oneShotPrefab;
        [SerializeField] 
        private Transform oneShotRoot;
        [SerializeField]
        [Min(1)] 
        private int oneShotMaxInstances = 32;

        private ObjectPool<AudioSource> oneShotPool;
        private readonly Dictionary<AudioSource, ActiveAudioSourceData> activeSources = new();
        private readonly Dictionary<AudioSource, CoroutineTask> releaseTasks = new();
        private readonly HashSet<FloatValue> subscribedVolumes = new();

        private struct ActiveAudioSourceData
        {
            public AudioModel Model;
            public float VolumeModifier;

            public ActiveAudioSourceData(AudioModel model, float volumeModifier)
            {
                Model = model;
                VolumeModifier = volumeModifier;
            }
        }

        #region MonoLifeCycle
        
        protected override void Awake()
        {
            base.Awake();

            SubscribeVolumeValues();

            if (oneShotPrefab == null) return;
            if (oneShotRoot == null) oneShotRoot = transform;

            oneShotPool = new ObjectPool<AudioSource>(
                oneShotPrefab,
                oneShotRoot,
                PREWARM_SOURCES_NUMBER,
                oneShotMaxInstances
            );
        }

        private void OnDestroy()
        {
            UnsubscribeVolumeValues();

            foreach (KeyValuePair<AudioSource, CoroutineTask> pair in releaseTasks)
            {
                if (pair.Value == null) continue;
                pair.Value.Stop();
            }

            releaseTasks.Clear();
            activeSources.Clear();
        }

        #endregion
        
        /// <summary>
        /// Воспроизвести аудио (2D по-умолчанию)
        /// </summary>
        /// <param name="resource">Аудио-ресурс</param>
        public void Play(AudioResource resource, AudioModel model = AudioModel.UI, AudioSpatialPreset spatialPreset = null)
        {
            if (spatialPreset == null) spatialPreset = GetSpatialDefaults(model);
            Play(resource, null, null, model, spatialPreset);
        }
        
        /// <summary>
        /// Воспроизвести аудио (в определенной точке, 3D по-умолчанию)
        /// </summary>
        /// <param name="resource">Аудио-ресурс</param>
        public void Play(AudioResource resource, Vector3 position, AudioModel model = AudioModel.Sfx, AudioSpatialPreset spatialPreset = null)
        {
            if (spatialPreset == null) spatialPreset = GetSpatialDefaults(model);
            Play(resource, position, null, model, spatialPreset);
        }

        /// <summary>
        /// Воспроизвести аудио (с закреплением за объектом, 3D по-умолчанию)
        /// </summary>
        /// <param name="resource">Аудио-ресурс</param>
        public void Play(AudioResource resource, Transform followTarget, AudioModel model = AudioModel.Sfx, AudioSpatialPreset spatialPreset = null)
        {
            if (spatialPreset == null) spatialPreset = GetSpatialDefaults(model);
            Play(resource, null, followTarget, model, spatialPreset);
        }
        
        #region Settings

        /// <summary>
        /// Получить дефолтные настройки в зависимости от типа аудио
        /// </summary>
        /// <param name="type">Тип аудио трека</param>
        /// <returns>Настройки аудио</returns>
        public AudioDefaults GetDefaults(AudioModel type)
        {
            if (type == AudioModel.Music) return musicDefaults;
            if (type == AudioModel.Ambience) return ambienceDefaults;
            if (type == AudioModel.UI) return uiDefaults;
            return sfxDefaults;
        }

        /// <summary>
        /// Получить текущее значение громкости для типа аудио
        /// </summary>
        /// <param name="model">Тип аудио</param>
        /// <returns>Текущее значение громкости</returns>
        public float GetVolume(AudioModel model)
        {
            AudioDefaults defaults = GetDefaults(model);
            float typeVolume = defaults.volume == null ? 1f : defaults.volume.Value;
            float globalVolume = commonVolume == null ? 1f : commonVolume.Value;
            return typeVolume * globalVolume;
        }

        /// <summary>
        /// Получить итоговую громкость для типа аудио
        /// </summary>
        /// <param name="model">Тип аудио</param>
        /// <param name="volumeModifier">Модификатор громкости</param>
        /// <returns>Итоговая громкость</returns>
        public float GetAppliedVolume(AudioModel model, float volumeModifier) => volumeModifier * GetVolume(model);

        /// <summary>
        /// Построение настроек аудио
        /// </summary>
        /// <param name="defaults">Дефолтные параметры аудио трека</param>
        /// <param name="loop">Зацикленность</param>
        /// <returns></returns>
        public AppliedAudioSettings BuildSettings(AudioDefaults defaults, bool loop = false)
        {
            AppliedAudioSettings settings = new();

            settings.mixerGroup = defaults.mixerGroup;
            settings.volume = defaults.volumeModifier;

            float minPitch = defaults.pitchMin;
            float maxPitch = defaults.pitchMax;
            if (maxPitch < minPitch)
            {
                (minPitch, maxPitch) = (maxPitch, minPitch);
            }

            settings.pitch = Random.Range(minPitch, maxPitch);

            settings.spatialBlend = 0;
            settings.minDistance = 0;
            settings.maxDistance = 0;
            settings.priority = defaults.priority;
            settings.loop = loop;

            return settings;
        }

        /// <summary>
        /// Применить конфигурацию настроек аудио к источнику аудио
        /// </summary>
        /// <param name="source"></param>
        /// <param name="settings"></param>
        public void ApplySettings(AudioSource source, AppliedAudioSettings settings)
        {
            if (source == null)
            {
                ServiceDebug.LogError("Источник не задан, настройки аудио не применены");
                return;
            }

            source.outputAudioMixerGroup = settings.mixerGroup;

            source.volume = settings.volume;
            source.pitch = settings.pitch;

            source.spatialBlend = settings.spatialBlend;

            source.minDistance = settings.minDistance;
            source.maxDistance = settings.maxDistance;

            source.priority = settings.priority;

            source.loop = settings.loop;
        }
        
        #endregion
        
        #region Internal

        private void Play(AudioResource resource, Vector3? position, Transform followTarget, AudioModel model, AudioSpatialPreset spatialPreset = null)
        {
            if (Logic.IsNull(resource, "Аудио-ресурс не назначен, аудио не воспроизведено")) return;
            if (!isPoolValid()) return;

            AudioSource source = oneShotPool.Get();
            if (source == null)
            {
                ServiceDebug.LogError("Ошибка создания источника звука, аудио не воспроизведено");
                return;
            }

            if (followTarget != null)
                source.transform.position = followTarget.position;
            else if (position.HasValue)
                source.transform.position = position.Value;

            AudioDefaults defaults = GetDefaults(model);
            AppliedAudioSettings settings = BuildSettings(defaults);
            float volumeModifier = defaults.volumeModifier;

            if (spatialPreset != null)
            {
                settings.spatialBlend = spatialPreset.SpatialBlend;
                settings.minDistance = spatialPreset.MinDistance;
                settings.maxDistance = spatialPreset.MaxDistance;
            }

            settings.volume = GetAppliedVolume(model, volumeModifier);

            ApplySettings(source, settings);
            RegisterActiveSource(source, model, volumeModifier);

            source.resource = resource;
            source.Play();

            if (followTarget != null)
            {
                ReleaseAfterPlayFollow(source, followTarget);
                return;
            }

            ReleaseAfterPlay(source);
        }
        
        private void ReleaseAfterPlay(AudioSource source)
        {
            if (Logic.IsNull(source, "Ошибка источника звука, аудио не воспроизведено")) return;
            if (!isPoolValid()) return;

            GetReleaseTask(source).Start(ReleaseRoutine(source, null));
        }

        private void ReleaseAfterPlayFollow(AudioSource source, Transform followTarget)
        {
            if (Logic.IsNull(source, "Ошибка источника звука, аудио не воспроизведено")) return;
            if (!isPoolValid()) return;

            GetReleaseTask(source).Start(ReleaseRoutine(source, followTarget));
        }

        private CoroutineTask GetReleaseTask(AudioSource source)
        {
            if (releaseTasks.TryGetValue(source, out CoroutineTask task)) return task;

            task = new CoroutineTask(this);
            releaseTasks.Add(source, task);
            return task;
        }

        private System.Collections.IEnumerator ReleaseRoutine(AudioSource source, Transform followTarget)
        {
            while (source.isPlaying)
            {
                if (followTarget != null) source.transform.position = followTarget.position;
                yield return null;
            }

            source.Stop();
            source.resource = null;
            source.loop = false;

            activeSources.Remove(source);
            oneShotPool.Release(source);

            if (releaseTasks.TryGetValue(source, out CoroutineTask task)) task.Stop();
        }

        private void RegisterActiveSource(AudioSource source, AudioModel model, float volumeModifier)
        {
            if (source == null) return;
            activeSources[source] = new ActiveAudioSourceData(model, volumeModifier);
        }

        private void SubscribeVolumeValues()
        {
            SubscribeVolume(commonVolume);
            SubscribeVolume(musicDefaults.volume);
            SubscribeVolume(ambienceDefaults.volume);
            SubscribeVolume(uiDefaults.volume);
            SubscribeVolume(sfxDefaults.volume);
        }

        private void SubscribeVolume(FloatValue volume)
        {
            if (volume == null) return;
            if (subscribedVolumes.Contains(volume)) return;

            volume.onValueChanged += RefreshActiveVolumes;
            subscribedVolumes.Add(volume);
        }

        private void UnsubscribeVolumeValues()
        {
            foreach (FloatValue volume in subscribedVolumes)
            {
                if (volume == null) continue;
                volume.onValueChanged -= RefreshActiveVolumes;
            }

            subscribedVolumes.Clear();
        }

        private void RefreshActiveVolumes(float _)
        {
            foreach (KeyValuePair<AudioSource, ActiveAudioSourceData> pair in activeSources)
            {
                if (pair.Key == null) continue;

                pair.Key.volume = GetAppliedVolume(pair.Value.Model, pair.Value.VolumeModifier);
            }
            onVolumeChanged?.Invoke();
        }

        private bool isPoolValid()
        {
            if (oneShotPool == null)
            {
                ServiceDebug.LogError("Ошибка пула, аудио не воспроизведено");
                return false;
            }

            return true;
        }

        private AudioSpatialPreset GetSpatialDefaults(AudioModel model)
        {
            AudioSpatialPreset preset = uiDefaults.spatialPreset;
            switch (model)
            {
                case AudioModel.Ambience:
                    preset = ambienceDefaults.spatialPreset;
                    break;
                case AudioModel.Music:
                    preset = musicDefaults.spatialPreset;
                    break;
                case AudioModel.Sfx:
                    preset = sfxDefaults.spatialPreset;
                    break;
                case AudioModel.UI:
                    preset = uiDefaults.spatialPreset;
                    break;
            }
            return preset;
        }
        
        #endregion
    }
}