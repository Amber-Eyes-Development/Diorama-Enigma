using System;
using Cysharp.Threading.Tasks;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data
{
    /// <summary>
    /// Вьюшка процесса сохранения: держит объект-индикатор включённым, пока JsonSaveLoad пишет данные
    /// <remarks>
    /// Индикатор должен быть отдельным объектом (не тем, на котором висит компонент),
    /// иначе его выключение отпишет вьюшку от событий
    /// </remarks>
    /// </summary>
    public class SaveIndicatorView : MonoBehaviour
    {
        [SerializeField] private GameObject indicator;
        [Tooltip("Минимальное время показа индикатора, чтобы он не мигал на коротких записях")]
        [SerializeField, Min(0f)] private float minShowSeconds = 0.5f;

        private int stateVersion;
        private float shownAtTime;

        private void Awake()
        {
            if (indicator == null)
            {
                ServiceDebug.LogError(this, "Не назначен объект-индикатор");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (indicator == null) return;

            JsonSaveLoad.onSavingStateChanged += OnSavingStateChanged;

            // Стартовая синхронизация — мгновенно, без минимального времени показа
            bool isSaving = JsonSaveLoad.IsSaving;
            stateVersion++;
            if (isSaving) shownAtTime = Time.unscaledTime;
            indicator.SetActive(isSaving);
        }

        private void OnDisable()
        {
            JsonSaveLoad.onSavingStateChanged -= OnSavingStateChanged;
        }

        private void OnSavingStateChanged(bool isSaving)
        {
            stateVersion++;

            if (isSaving)
            {
                shownAtTime = Time.unscaledTime;
                indicator.SetActive(true);
                return;
            }

            float remainingShowTime = minShowSeconds - (Time.unscaledTime - shownAtTime);
            if (remainingShowTime <= 0f)
            {
                indicator.SetActive(false);
                return;
            }

            HideDelayedAsync(stateVersion, remainingShowTime).Forget();
        }

        private async UniTaskVoid HideDelayedAsync(int version, float delaySeconds)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), DelayType.Realtime,
                cancellationToken: this.GetCancellationTokenOnDestroy());

            // За время ожидания началось новое сохранение — скрытие уже неактуально
            if (version != stateVersion) return;

            indicator.SetActive(false);
        }
    }
}
