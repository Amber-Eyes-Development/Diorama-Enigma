using System;
using UnityEngine;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Базовый вспомогательный класс контекста выбора InMemoryData-контейнера
    /// </summary>
    public abstract class BaseSelectionContext : ScriptableObject
    {
        /// <summary>
        /// Событие изменения активного элемента
        /// </summary>
        public event Action onSelectionChanged;

        /// <summary>
        /// Наличие активного выбранного элемента
        /// </summary>
        public abstract bool HasSelection { get; }
        
        /// <summary>
        /// Очистка выбора
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Задание выбора
        /// </summary>
        public abstract void Select(string selectionDataId);
        
        protected void OnSelectionChanged() => onSelectionChanged?.Invoke();
    }
}