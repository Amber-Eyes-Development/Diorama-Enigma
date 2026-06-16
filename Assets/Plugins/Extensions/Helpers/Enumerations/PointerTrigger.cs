namespace Extensions.Helpers.Enumerations
{
    /// <summary>
    /// Варианты триггеров указателя
    /// </summary>
    public enum PointerTrigger
    {
        /// <summary> Полный клик (вход, нажатие, отжатие) </summary>
        Click = 0,
        /// <summary> Вход указателя </summary>
        Enter = 1,
        /// <summary> Выход указателя </summary>
        Exit = 2,
        /// <summary> Нажатие </summary>
        Down = 3,
        /// <summary> Отжатие </summary>
        Up = 4
    }
}