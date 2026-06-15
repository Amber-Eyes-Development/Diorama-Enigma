using System.Collections;
using UnityEngine;
using Extensions.Coroutines;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine.Audio;

namespace Extensions.Audio
{
    /// <summary>
    /// Плеер зацикленного аудио
    /// </summary>
    public class AudioLoopPlayer : BaseAudioPlayer
    {
        /// <summary>
        /// Источник зацикленного аудио
        /// </summary>
        public AudioSource Source => source;

        [Header("Звуки")]
        
        [SerializeField]
        protected AudioResource audioResource;
        [SerializeField]
        protected bool playOnEnable;
        [SerializeField]
        protected float playFadeSeconds;

        protected AudioSource source;
        protected CoroutineTask task;
        protected CoroutineTask pitchTask;

        protected int token;

        protected virtual void Awake()
        {
            task = new CoroutineTask(this);
            pitchTask = new CoroutineTask(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (!playOnEnable) return;
            if (Logic.IsNull(audioResource, "Ошибка аудио ресурса, цикл не воспроизведен")) return;
            SetLoop(audioResource, playFadeSeconds);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Stop();
        }

        protected virtual void OnDestroy()
        {
            if (task != null)
            {
                task.Stop();
            }

            if (pitchTask != null)
            {
                pitchTask.Stop();
            }

            if (source != null)
            {
                Destroy(source.gameObject);
            }
        }

        /// <summary>
        /// Начать воспроизведение зацикленного аудио
        /// </summary>
        /// <param name="resource">Аудио трек</param>
        /// <param name="fadeSeconds">Время затухания</param>
        public void SetLoop(AudioResource resource, float fadeSeconds = 0f)
        {
            if (!isActiveAndEnabled) return;
            if (Logic.IsNull(resource, "Ошибка аудио ресурса, цикл не воспроизведен")) return;

            token++;
            if (token == 0) token = 1;

            EnsureSource();

            task.Start(SetLoopRoutine(resource, fadeSeconds, token));
        }

        /// <summary>
        /// Остановить зацикленное аудио
        /// </summary>
        /// <param name="fadeSeconds">Время затухания</param>
        public void Stop(float fadeSeconds = 0f)
        {
            if (Logic.IsNull(source, "Ошибка источника аудио, цикл не остановлен")) return;

            token++;
            if (token == 0) token = 1;

            task.Start(StopRoutine(fadeSeconds, token));
        }

        protected void EnsureSource()
        {
            if (source != null) return;

            GameObject sourceObject = new GameObject("AudioLoopSource");
            sourceObject.transform.SetParent(transform, false);

            source = sourceObject.AddComponent<AudioSource>();
        }
        
        protected override void OnVolumeSettingsChanged()
        {
            if (source == null) return;
            if (!source.isPlaying) return;

            source.volume = audioController.GetAppliedVolume(model, defaults.volumeModifier);
        }
        
        protected IEnumerator SetLoopRoutine(
            AudioResource resource,
            float fadeSeconds,
            int localToken)
        {
            if (resource == null) yield break;
            if (source == null) yield break;

            if (fadeSeconds > 0f && source.isPlaying)
            {
                float start = source.volume;
                float time = 0f;

                while (time < fadeSeconds)
                {
                    if (localToken != token) yield break;

                    time += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(time / fadeSeconds);
                    source.volume = Mathf.Lerp(start, 0f, t);
                    yield return null;
                }
            }

            if (localToken != token) yield break;

            if (audioController == null)
            {
                ServiceDebug.LogError("Контроллер аудио не найден");
                yield break;
            }
                
            AudioDefaults defaults = audioController.GetDefaults(model);
            AppliedAudioSettings settings = audioController.BuildSettings(defaults, true);
            settings.spatialBlend = spatialPreset.SpatialBlend;
            settings.minDistance = spatialPreset.MinDistance;
            settings.maxDistance = spatialPreset.MaxDistance;
            settings.volume = audioController.GetAppliedVolume(model, defaults.volumeModifier);

            audioController.ApplySettings(source, settings);

            source.resource = resource;
            source.loop = true;

            float targetVolume = settings.volume;

            source.volume = 0f;
            source.Play();

            pitchTask.Start(PitchRoutine(localToken));

            if (fadeSeconds <= 0f)
            {
                source.volume = targetVolume;
                yield break;
            }

            float fadeTime = 0f;
            while (fadeTime < fadeSeconds)
            {
                if (localToken != token) yield break;

                fadeTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeTime / fadeSeconds);
                source.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            source.volume = targetVolume;
        }

        protected System.Collections.IEnumerator StopRoutine(
            float fadeSeconds,
            int localToken)
        {
            if (source == null) yield break;
            if (!source.isPlaying) yield break;

            if (fadeSeconds <= 0f)
            {
                source.Stop();
                source.resource = null;
                source.loop = false;
                yield break;
            }

            float start = source.volume;
            float time = 0f;

            while (time < fadeSeconds)
            {
                if (localToken != token) yield break;

                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / fadeSeconds);
                source.volume = Mathf.Lerp(start, 0f, t);
                yield return null;
            }

            if (localToken != token) yield break;

            source.Stop();
            source.volume = start;
            source.resource = null;
            source.loop = false;
        }

        /// <summary> Обновлять питч на каждом новом цикле, пока актуален текущий запуск </summary>
        protected IEnumerator PitchRoutine(int localToken)
        {
            if (source == null || audioController == null) yield break;

            AudioDefaults defaults = audioController.GetDefaults(model);
            int lastSample = source.timeSamples;

            while (localToken == token && source != null && source.isPlaying)
            {
                int sample = source.timeSamples;
                if (sample < lastSample) source.pitch = AudioController.RandomPitch(defaults);
                lastSample = sample;

                yield return null;
            }
        }
    }
}
