using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Загрузка InMemory БД на OnEnable
    /// <remarks>
    /// Используется для прогрева БД до использования
    /// Поддерживает как синхронную, так и асинхронную загрузку
    /// </remarks>
    /// </summary>
    public class InMemoryDataEnableLoader : MonoBehaviour
    {
        [SerializeField]
        protected List<InMemoryDataBaseObject> dataBases = new();

        [SerializeField]
        [Tooltip("Использовать асинхронную предзагрузку (рекомендуется для больших данных)")]
        protected bool useAsyncPreload = true;

        [SerializeField]
        [Tooltip("Показывать лог загрузки")]
        protected bool showLoadingLog = false;

        protected virtual async void OnEnable()
        {
            if (useAsyncPreload)
            {
                await PreloadAsync();
            }
            else
            {
                PreloadSync();
            }
        }

        /// <summary>
        /// Асинхронная предзагрузка (не блокирует UI)
        /// </summary>
        private async UniTask PreloadAsync()
        {
            if (showLoadingLog)
            {
                ServiceDebug.Log($"Начало асинхронной загрузки {dataBases.Count} БД...");
            }

            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                await dataBase.PreloadAsync();
                
                if (showLoadingLog)
                {
                    ServiceDebug.Log($"Загружена БД: {dataBase.name}");
                }
            }

            if (showLoadingLog)
            {
                ServiceDebug.Log($"Все БД загружены");
            }
        }

        /// <summary>
        /// Синхронная загрузка (может блокировать кадр при первом обращении)
        /// </summary>
        private void PreloadSync()
        {
            if (showLoadingLog)
            {
                ServiceDebug.Log($"Начало синхронной загрузки {dataBases.Count} БД...");
            }

            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                dataBase.EnsureLoaded();
                
                if (showLoadingLog)
                {
                    ServiceDebug.Log($"Загружена БД: {dataBase.name}");
                }
            }

            if (showLoadingLog)
            {
                ServiceDebug.Log($"Все БД загружены");
            }
        }

        /// <summary>
        /// Ручная предзагрузка извне (для контроля последовательности)
        /// </summary>
        public async UniTask LoadAllAsync() => await PreloadAsync();
    }
}