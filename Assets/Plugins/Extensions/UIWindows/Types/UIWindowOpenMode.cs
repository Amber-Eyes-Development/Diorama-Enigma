namespace Extensions.UIWindows
{
    /// <summary>
    /// Режим открытия окна
    /// </summary>
    public enum UIWindowOpenMode
    {
        /// <summary>
        /// Обычный переход вперёд
        /// </summary>
        Forward = 0,
        /// <summary>
        /// Возврат к окну через обрезку хвоста истории
        /// </summary>
        Pop = 1
    }
}