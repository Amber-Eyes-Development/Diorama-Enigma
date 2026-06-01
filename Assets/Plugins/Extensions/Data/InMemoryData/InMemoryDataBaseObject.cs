using System;
using Cysharp.Threading.Tasks;
using Extensions.Identification;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Базовый класс InMemory хранилища данных
    /// </summary>
    // TODO Переименовать в InMemoryBaseContainer?
    public abstract class InMemoryDataBaseObject<TData> : InMemoryDataBaseObject where TData : new()
    {
        #region Events
        
        /// <summary>
        /// Событие загрузки данных хранилища
        /// </summary>
        public event Action onDataLoaded;
        /// <summary>
        /// Событие сохранения хранилища
        /// </summary>
        public event Action onDataSaved;
        /// <summary>
        /// Событие ошибки сохранения хранилища 
        /// </summary>
        public event Action onDataSaveError;
        /// <summary>
        /// Событие обновления данных хранилища
        /// </summary>
        public event Action onDataUpdated;
        
        #endregion
        
        [Header("Сохранение данных контейнера"), Space]
        [SerializeField] protected bool autoSave = true;
        
        protected virtual string SaveProfile => null;
        
        /// <summary>
        /// Хранимые данные
        /// </summary>
        public virtual TData Data
        {
            get
            {
                EnsureLoaded();
                
                return data;
            }
            set
            {
                EnsureLoaded();
                
                data = value;
                MarkDirty();
            }
        }

        [NonSerialized] protected TData data;
        
        [NonSerialized] protected bool loaded;
        [NonSerialized] protected bool dirty;
        
        /// <summary>
        /// Гарантированная загрузка данных (синхронно через кэш)
        /// </summary>
        public override void EnsureLoaded()
        {
            if (loaded) return;

            OnInitialize();

            if (JsonSaveLoad.Exists(id, SaveProfile))
            {
                data = JsonSaveLoad.Load(id, default(TData), SaveProfile);

                if (data == null)
                {
                    ServiceDebug.LogError($"Не удалось загрузить данные контейнера {name}, созданы данные по-умолчанию");
                    data = new TData();
                }
            }
            else
            {
                ServiceDebug.LogWarning($"Данные контейнера {name} не найдены, созданы данные по-умолчанию");
                data = new TData();
            }

            loaded = true;
            dirty = false;
            OnDataLoaded();
        }

        /// <summary>
        /// Метод, вызываемый после первой инициализации
        /// </summary>
        protected virtual void OnDataLoaded() => onDataLoaded?.Invoke();
        
        #region  Save/Load

        /// <summary>
        /// Запрос сохранения таблицы (синхронный)
        /// </summary>
        /// <returns>Сохранена ли таблица</returns>
        public override bool RequestSave()
        {
            EnsureLoaded();
            return Save();
        }

        /// <summary>
        /// Запрос сохранения данных (асинхронный)
        /// </summary>
        /// <returns>Сохранены ли данные</returns>
        public override async UniTask<bool> RequestSaveAsync()
        {
            EnsureLoaded();
            return await SaveAsync();
        }

        // forced использовать с умом: может затереть сохраненные данные
        protected bool Save(bool forced = false)
        {
            if (!forced && !loaded)
            {
                ServiceDebug.LogWarning($"Попытка сохранения еще не загруженной таблицы {name}");
                return false;
            }
            
            if (!forced && !dirty)
            {
                return false;
            }

            // Синхронное сохранение через кэш
            if (JsonSaveLoad.Save(data, id, SaveProfile))
            {
                dirty = false;
                onDataSaved?.Invoke();
                return true;
            }
            else
            {
                onDataSaveError?.Invoke();
                return false;
            }
        }

        protected async UniTask<bool> SaveAsync()
        {
            if (!loaded)
            {
                ServiceDebug.LogWarning($"Попытка сохранения еще не загруженной таблицы {name}");
                return false;
            }
            
            if (!dirty)
            {
                return false;
            }

            if (await JsonSaveLoad.SaveAsync(data, id, SaveProfile))
            {
                dirty = false;
                onDataSaved?.Invoke();
                return true;
            }
            else
            {
                onDataSaveError?.Invoke();
                return false;
            }
        }

        /// <summary>
        /// Предзагрузка данных асинхронно
        /// </summary>
        public override async UniTask PreloadAsync()
        {
            if (loaded)
            {
                return;
            }

            await JsonSaveLoad.PreloadAsync(id, new TData(), SaveProfile);
            
            // После preload данные уже в кэше, можем загрузить синхронно
            EnsureLoaded();
        }


        #region Internal

        protected virtual void MarkDirty(bool notify = true)
        {
            dirty = true;
            
            if (notify) onDataUpdated?.Invoke();

            if (autoSave) Save();
        }

        #endregion
        
        protected virtual void OnInitialize() { }

        #endregion
    }

    /// <summary>
    /// Абстракция
    /// </summary>
    public abstract class InMemoryDataBaseObject : IdentifiableObject
    {
        public abstract bool RequestSave();
        public abstract UniTask  PreloadAsync();
        public abstract UniTask<bool> RequestSaveAsync();
        public abstract void EnsureLoaded();
    }
}