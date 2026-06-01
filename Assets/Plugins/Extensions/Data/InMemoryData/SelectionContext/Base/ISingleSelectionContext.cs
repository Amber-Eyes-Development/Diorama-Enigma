namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Контракт данных контекста одиночного выбора
    /// </summary>
    public interface ISingleSelectionContext
    {
        /// <summary>
        /// Идентификатор активной выбранной записи контейнера
        /// </summary>
        public string SelectedId { get; }
        
        /// <summary>
        /// Выбрать данные как активные
        /// </summary>
        /// <param name="dataItemId">Идентификатор</param>
        public void Select(string selectionDataId);
    }
}