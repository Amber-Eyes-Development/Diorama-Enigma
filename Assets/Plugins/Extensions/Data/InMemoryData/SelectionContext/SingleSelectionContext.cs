using System.Linq;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Контекст выбора <see cref="InMemoryData"/>-контейнера
    /// </summary>
    /// <typeparam name="TData">Тип хранимых данных</typeparam>
    public abstract class SingleSelectionContext<TData> : BaseSelectionContext, ISingleSelectionContext 
        where TData : InMemoryDataEntry
    {
        /// <summary>
        /// Контейнер, данные которого назначаются как активный выбор контекста
        /// </summary>
        [field:Header("Контейнер данных для выбора"), Space]
        [field: SerializeField]
        public InMemoryDataContainer<TData> Container { get; private set; }

        /// <summary>
        /// Есть ли выбор
        /// </summary>
        public override bool HasSelection => IsContainerInited() && !string.IsNullOrEmpty(SelectedId);
        
        /// <summary>
        /// Идентификатор активной выбранной записи контейнера
        /// </summary>
        public string SelectedId => selectedId;

        [Header("Превью выбранной единицы данных"), Space]
        [SerializeField]
        protected string selectedId;
        
        /// <summary>
        /// Выбрать данные как активные
        /// </summary>
        /// <param name="dataItemId">Идентификатор</param>
        public override void Select(string selectionDataId)
        {
            bool contains = Container.Data.Any(x => x.Id == selectionDataId);
            if (!contains)
            {
                ServiceDebug.LogWarning($"Запись с идентификатором «{selectionDataId}» отсутствует в «{Container.name}», " +
                                      "контекст выбора очищен");
                Clear();
                return;
            }
            
            selectedId = selectionDataId;
            OnSelectionChanged();
        }

        /// <summary>
        /// Очистить выбор
        /// </summary>
        public override void Clear()
        {
            selectedId = string.Empty;
            OnSelectionChanged();
        }

        #region Получение данных

        /// <summary>
        /// Получить активные данные
        /// </summary>
        /// <returns>Выбранные контекстом данные</returns>
        public TData GetSelectedData()
        {
            if (!HasSelection) return null;

            Container.GetById(SelectedId, out TData dataItem);
            return dataItem;
        }

        /// <summary>
        /// Попытка получить активные данные
        /// </summary>
        /// <param name="dataItem">out: выбранные контекстом данные</param>
        /// <returns>true если данные в контейнере успешно найдены, иначе false</returns>
        public bool TryGetSelectedData(out TData dataItem)
        {
            dataItem = null;

            if (!HasSelection || !IsContainerInited()) return false;

            return Container.GetById(SelectedId, out dataItem);
        }

        #endregion

        private bool IsContainerInited()
        {
            if (Container == null)
            {
                ServiceDebug.LogError("Контейнер не назначен, данные невозможно получить");
                return false;
            }

            return true;
        }
    }
}