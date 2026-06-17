using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Сохранение InMemory БД при сворачивании, потере фокуса и выходе из приложения
    /// </summary>
    /// <remarks>
    /// Выход обрабатывается через Application.wantsToQuit: данные синхронно помечаются к записи, а сам выход
    /// откладывается до завершения батч-записи JsonSaveLoad. Это надёжнее, чем await в async void OnApplicationQuit —
    /// Unity не ждёт продолжение после возврата из lifecycle-колбэка, и фактическая запись может не выполниться.
    /// </remarks>
    public class InMemoryDataPauseSaver : MonoBehaviour
    {
        [SerializeField] protected List<InMemoryDataBaseObject> dataBases = new();

        [Tooltip("Сохранять при выходе из приложения (через Application.wantsToQuit)")]
        [SerializeField] protected bool saveOnQuit = true;

        [Tooltip("Также сохранять при потере фокуса (OnApplicationFocus)")]
        [SerializeField] protected bool saveOnFocusLost = false;

        [Tooltip("Показывать лог сохранения")]
        [SerializeField] protected bool showSaveLog = false;

        private bool quitFlushStarted;

        protected virtual void OnEnable()
        {
            Application.wantsToQuit -= HandleWantsToQuit;
            Application.wantsToQuit += HandleWantsToQuit;
        }

        protected virtual void OnDisable() => Application.wantsToQuit -= HandleWantsToQuit;

        protected virtual void OnApplicationPause(bool pause)
        {
            if (!pause) return;

            if (showSaveLog) ServiceDebug.Log("Приложение на паузе, сохранение данных");
            MarkAllForSave();
            JsonSaveLoad.FlushAsync().Forget();
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus || !saveOnFocusLost) return;

            if (showSaveLog) ServiceDebug.Log("Приложение потеряло фокус, сохранение данных");
            MarkAllForSave();
            JsonSaveLoad.FlushAsync().Forget();
        }

        /// <summary>
        /// Ручное сохранение всех БД (без ожидания записи на диск)
        /// </summary>
        public void SaveAllManual() => MarkAllForSave();

        /// <summary>
        /// Ручное сохранение всех БД с ожиданием фактической записи на диск
        /// </summary>
        /// <returns>Количество сохранённых БД</returns>
        public async UniTask<int> SaveAllManualAsync()
        {
            int savedCount = 0;
            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                if (await dataBase.RequestSaveAsync()) savedCount++;
            }

            return savedCount;
        }

        #region Internal

        /// <summary> Синхронно проталкивает данные всех БД в очередь записи JsonSaveLoad </summary>
        private void MarkAllForSave()
        {
            foreach (InMemoryDataBaseObject dataBase in dataBases)
                if (dataBase != null) dataBase.RequestSave();
        }

        /// <summary>
        /// Надёжный момент дозаписать данные при выходе: помечаем к записи и откладываем выход (return false)
        /// до завершения батч-записи; после флаша вызываем Application.Quit повторно
        /// </summary>
        private bool HandleWantsToQuit()
        {
            if (quitFlushStarted || !saveOnQuit) return true;

            MarkAllForSave();
            if (!JsonSaveLoad.HasPendingSaves) return true;

            quitFlushStarted = true;
            QuitAfterFlushAsync().Forget();
            return false;
        }

        private async UniTaskVoid QuitAfterFlushAsync()
        {
            try
            {
                await JsonSaveLoad.FlushAsync();
                if (showSaveLog) ServiceDebug.Log("Сохранения записаны, выход из приложения");
            }
            catch (Exception e)
            {
                ServiceDebug.LogError($"Ошибка записи сохранений при выходе: {e}");
            }

            Application.Quit();
        }

        #endregion
    }
}
