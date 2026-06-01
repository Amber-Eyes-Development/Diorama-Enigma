using System.Collections.Generic;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Контракт данных контекста множественного выбора
    /// </summary>
    public interface IMultipleSelectionContext
    {
        /// <summary>
        /// Идентификатор активной выбранной записи контейнера
        /// </summary>
        public IReadOnlyList<string> SelectedIds { get; }
        
        /// <summary>
        /// Выбрать данные как активные
        /// </summary>
        /// <param name="dataItemId">Идентификатор</param>
        public void Select(string selectionDataId);
    }
}