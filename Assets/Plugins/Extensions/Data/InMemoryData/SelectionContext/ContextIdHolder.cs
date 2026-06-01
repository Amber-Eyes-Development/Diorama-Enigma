using System;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Хранилище идентификатора связанной с объектом единицы данных
    /// </summary>
    public abstract class ContextIdHolder<TContainer, TEntry> : ContextIdHolder
        where TContainer : InMemoryDataContainer<TEntry> 
        where TEntry : InMemoryDataEntry
    {
        /// <summary>
        /// Прилинкованный контейнер данных элемента
        /// </summary>
        public TContainer DataContainer => dataContainer;
        
        protected TContainer dataContainer;
        
        /// <summary>
        /// Инициализация данных
        /// </summary>
        /// <param name="id">Идентификатор единицы данных</param>
        public virtual void Initialize(TContainer newContainer, string newItemCellId = null)
        {
            if (newContainer == null)
            {
                ServiceDebug.LogError("Ссылка на контейнер данных отсутствует, инициализация прервана");
                return;
            }
            
            dataContainer = newContainer;
            base.Initialize(newItemCellId);
        }
    }

    /// <summary>
    /// База контекста идентификатора
    /// </summary>
    public class ContextIdHolder : MonoBehaviour
    {
        /// <summary>
        /// Событие инициализации
        /// </summary>
        public event Action onInitialized;

        /// <summary>
        /// Связный идентификатор
        /// </summary>
        public string EntryId => entryId;
        /// <summary>
        /// Состояние инициализации
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        protected string entryId = string.Empty;
        protected bool isInitialized = false;
        
        protected virtual void OnInitialized() => onInitialized?.Invoke();
        
        /// <summary>
        /// Инициализация данных
        /// </summary>
        /// <param name="id">Идентификатор единицы данных</param>
        public virtual void Initialize(string newItemCellId = null)
        {
            entryId = newItemCellId;
            isInitialized = true;
            OnInitialized();
        }
    }
}