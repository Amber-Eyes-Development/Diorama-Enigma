namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Тип события, на которое реагирует вьюшка
    /// </summary>
    public enum TriggerKind
    {
        /// <summary> Курсор наведён </summary>
        HoverEnter,
        /// <summary> Курсор ушёл </summary>
        HoverExit,
        /// <summary> Объект вошёл в стейт </summary>
        StateEntered,
    }
}
