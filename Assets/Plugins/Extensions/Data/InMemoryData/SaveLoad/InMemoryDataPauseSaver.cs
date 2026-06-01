using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Сохранение InMemory БД при остановке приложения на паузу или выходе
    /// <remarks>
    /// Используется для своевременного сохранения данных
    /// </remarks>
    /// </summary>
    public class InMemoryDataPauseSaver : MonoBehaviour
    {
        [SerializeField]
        protected List<InMemoryDataBaseObject> dataBases = new();

        [SerializeField]
        [Tooltip("Ждать завершения сохранения при паузе (рекомендуется)")]
        protected bool waitForSaveCompletion = true;

        [SerializeField]
        [Tooltip("Также сохранять при OnApplicationQuit")]
        protected bool saveOnQuit = true;

        [SerializeField]
        [Tooltip("Также сохранять при OnApplicationFocus (потеря фокуса)")]
        protected bool saveOnFocusLost = false;

        [SerializeField]
        [Tooltip("Показывать лог сохранения")]
        protected bool showSaveLog = false;

        [SerializeField]
        [Tooltip("Таймаут ожидания сохранения в секундах (для OnQuit)")]
        protected float saveTimeoutSeconds = 5f;

        protected virtual async void OnApplicationPause(bool pause)
        {
            if (!pause) return;

            if (showSaveLog)
            {
                ServiceDebug.Log($"Приложение на паузе, сохранение данных...");
            }

            if (waitForSaveCompletion)
            {
                await SaveAllAsync();
            }
            else
            {
                SaveAllSync();
            }
        }

        protected virtual async void OnApplicationQuit()
        {
            if (!saveOnQuit) return;

            if (showSaveLog)
            {
                ServiceDebug.Log($"Выход из приложения, сохранение данных...");
            }

            // При OnQuit всегда ждем завершения с таймаутом
            await SaveAllWithTimeoutAsync();
        }

        protected virtual async void OnApplicationFocus(bool hasFocus)
        {
            if (!saveOnFocusLost || hasFocus) return;

            if (showSaveLog)
            {
                ServiceDebug.Log($"Приложение потеряло фокус, сохранение данных...");
            }

            if (waitForSaveCompletion)
            {
                await SaveAllAsync();
            }
            else
            {
                SaveAllSync();
            }
        }

        /// <summary>
        /// Асинхронное сохранение всех БД
        /// </summary>
        private async UniTask SaveAllAsync()
        {
            int savedCount = 0;
            int totalCount = 0;

            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                totalCount++;
                bool success = await dataBase.RequestSaveAsync();
                
                if (success)
                {
                    savedCount++;
                }
            }

            if (showSaveLog)
            {
                ServiceDebug.Log($"Сохранено {savedCount}/{totalCount} БД");
            }
        }

        /// <summary>
        /// Асинхронное сохранение с таймаутом (для OnQuit)
        /// </summary>
        private async UniTask SaveAllWithTimeoutAsync()
        {
            var saveTask = SaveAllAsync();
            var timeoutTask = UniTask.Delay(System.TimeSpan.FromSeconds(saveTimeoutSeconds));

            var completedTask = await UniTask.WhenAny(saveTask, timeoutTask);

            if (completedTask == 0)
            {
                if (showSaveLog)
                {
                    ServiceDebug.Log($"Сохранение успешно завершено");
                }
            }
            else
            {
                ServiceDebug.LogWarning($"Таймаут сохранения ({saveTimeoutSeconds}s)! Некоторые данные могут быть не сохранены");
            }
        }

        /// <summary>
        /// Синхронное сохранение (fire-and-forget)
        /// </summary>
        private void SaveAllSync()
        {
            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                dataBase.RequestSave();
            }

            if (showSaveLog)
            {
                ServiceDebug.Log($"Запущено сохранение {dataBases.Count} БД (fire-and-forget)");
            }
        }

        /// <summary>
        /// Ручное сохранение всех БД извне
        /// </summary>
        public async UniTask<int> SaveAllManualAsync()
        {
            int savedCount = 0;
            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                if (await dataBase.RequestSaveAsync())
                {
                    savedCount++;
                }
            }
            return savedCount;
        }

        /// <summary>
        /// Ручное сохранение всех БД извне (fire-and-forget)
        /// </summary>
        public void SaveAllManual() => SaveAllSync();
    }
}