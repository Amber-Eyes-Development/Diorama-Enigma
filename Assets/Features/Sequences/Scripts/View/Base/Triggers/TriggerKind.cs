namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Тип события шага, на которое реагирует вьюшка
    /// </summary>
    public enum TriggerKind
    {
        /// <summary> Шаг завершён: например пар при докрученном вентиле </summary>
        Completed = 0,
        /// <summary> Шаг не завершён </summary>
        NotCompleted = 1,
        /// <summary> Шаг стал доступен для изменения: открыть кожух над кнопкой </summary>
        Unlocked = 2,
        /// <summary> Шаг стал недоступен для изменения </summary>
        Locked = 3,
    }
}
