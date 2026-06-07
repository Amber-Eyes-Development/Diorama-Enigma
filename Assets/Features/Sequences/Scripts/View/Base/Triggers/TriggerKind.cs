namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Тип события шага, на которое реагирует вьюшка
    /// </summary>
    public enum TriggerKind
    {
        /// <summary> Значение стало true (вкл): например пар при кручении вентиля </summary>
        StateOn = 0,
        /// <summary> Значение стало false (выкл) </summary>
        StateOff = 1,
        /// <summary> Шаг стал доступен для изменения: открыть кожух над кнопкой </summary>
        Unlocked = 2,
        /// <summary> Шаг стал недоступен для изменения </summary>
        Locked = 3,
    }
}
