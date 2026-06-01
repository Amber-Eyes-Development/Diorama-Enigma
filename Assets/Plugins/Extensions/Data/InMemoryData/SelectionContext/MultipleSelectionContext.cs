using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Контекст множественного выбора <see cref="InMemoryData"/>-контейнера
    /// </summary>
    /// <typeparam name="TData">Тип хранимых данных</typeparam>
    public abstract class MultipleSelectionContext<TData> : BaseSelectionContext, IMultipleSelectionContext
        where TData : InMemoryDataEntry
    {
        /// <summary>
        /// Идентификаторы активных выбранных записей контейнера
        /// </summary>
        public IReadOnlyList<string> SelectedIds => selectedIds;

        public override bool HasSelection => Container != null && selectedIds.Count > 0;
        /// <summary>
        /// Количество выбранных элементов
        /// </summary>
        public int SelectionsNumber => selectedIds.Count;
        
        /// <summary>
        /// Контейнер, данные которого назначаются как активный выбор контекста
        /// </summary>
        [field:Header("Контейнер данных для выбора"), Space]
        [field: SerializeField]
        public InMemoryDataContainer<TData> Container { get; private set; }

        [Header("Превью списка выбранных данных"), Space]
        [SerializeField]
        protected List<string> selectedIds = new List<string>();

        /// <summary>
        /// Задание выбора
        /// </summary>
        /// <param name="selectionDataId">Идентификатор</param>
        public override void Select(string selectionDataId)
        {
            selectedIds.Clear();

            if (string.IsNullOrEmpty(selectionDataId))
            {
                OnSelectionChanged();
                return;
            }

            if (!IsContainerInited())
            {
                OnSelectionChanged();
                return;
            }

            if (!Container.GetById(selectionDataId, out _))
            {
                ServiceDebug.LogWarning($"Запись с идентификатором «{selectionDataId}» отсутствует в «{Container.name}», " +
                                      "контекст выбора очищен");
                OnSelectionChanged();
                return;
            }

            selectedIds.Add(selectionDataId);

            OnSelectionChanged();
        }

        /// <summary>
        /// Очистка выбора
        /// </summary>
        public override void Clear()
        {
            selectedIds.Clear();

            OnSelectionChanged();
        }

        /// <summary>
        /// Выбрать данные как активные
        /// </summary>
        /// <param name="container">Контейнер данных</param>
        /// <param name="dataItemIds">Идентификаторы данных</param>
        public void Select(InMemoryDataContainer<TData> container, IReadOnlyList<string> dataItemIds)
        {
            Container = container;
            this.selectedIds.Clear();

            if (dataItemIds == null)
            {
                OnSelectionChanged();
                return;
            }

            if (!IsContainerInited())
            {
                OnSelectionChanged();
                return;
            }

            HashSet<string> addedIds = new HashSet<string>();

            foreach (var dataItemId in dataItemIds)
            {
                if (string.IsNullOrEmpty(dataItemId)|| addedIds.Contains(dataItemId)) continue;

                if (!Container.GetById(dataItemId, out _))
                {
                    ServiceDebug.LogWarning($"Запись с идентификатором «{dataItemId}» отсутствует в «{Container.name}», " +
                                            "запись пропущена");
                    continue;
                }

                addedIds.Add(dataItemId);
                this.selectedIds.Add(dataItemId);
            }

            OnSelectionChanged();
        }

        /// <summary>
        /// Выбрать данные как активные
        /// </summary>
        /// <param name="container">Контейнер данных</param>
        /// <param name="dataItems">Данные</param>
        public void Select(InMemoryDataContainer<TData> container, IReadOnlyList<TData> dataItems)
        {
            Container = container;
            selectedIds.Clear();

            if (dataItems == null)
            {
                OnSelectionChanged();
                return;
            }

            if (!IsContainerInited())
            {
                OnSelectionChanged();
                return;
            }

            HashSet<string> addedIds = new HashSet<string>();

            foreach (var dataItem in dataItems)
            {
                if (dataItem == null || 
                    string.IsNullOrEmpty(dataItem.Id) ||
                    addedIds.Contains(dataItem.Id))
                {
                    continue;
                }

                if (!Container.GetById(dataItem.Id, out _))
                {
                    ServiceDebug.LogWarning($"Запись с идентификатором «{dataItem.Id}» отсутствует в «{Container.name}», " +
                                            "запись пропущена");
                    continue;
                }

                addedIds.Add(dataItem.Id);
                selectedIds.Add(dataItem.Id);
            }

            OnSelectionChanged();
        }

        /// <summary>
        /// Получить активные данные
        /// </summary>
        /// <param name="dataItems">Активные данные</param>
        /// <returns>true если есть выбранные данные, иначе false</returns>
        public bool TryGetSelected(List<TData> dataItems)
        {
            if (dataItems == null) return false;
            dataItems.Clear();

            if (!HasSelection) return false;

            foreach (var dataItemId in selectedIds)
            {
                if (Container.GetById(dataItemId, out TData dataItem))
                {
                    dataItems.Add(dataItem);
                    continue;
                }

                ServiceDebug.LogWarning($"Запись с идентификатором «{dataItemId}» отсутствует в «{Container.name}»");
            }

            return dataItems.Count > 0;
        }

        protected bool IsContainerInited()
        {
            if (Container != null) return true;
            
            ServiceDebug.LogError("Контейнер не назначен, данные невозможно получить");
            return false;
        }
    }
}