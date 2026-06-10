namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Режим завершения композитного шага по дочерним шагам
    /// </summary>
    public enum CompletionMode
    {
        /// <summary> Завершены все дочерние шаги </summary>
        All,
        /// <summary> Завершён хотя бы один </summary>
        Any,
        /// <summary> Завершено не менее N </summary>
        AtLeast,
        /// <summary> Завершено не более N </summary>
        AtMost,
        /// <summary> Завершено ровно N </summary>
        Exactly,
        /// <summary> Не завершён ни один </summary>
        None,
        /// <summary> Завершены не все </summary>
        NotAll,
    }
}
