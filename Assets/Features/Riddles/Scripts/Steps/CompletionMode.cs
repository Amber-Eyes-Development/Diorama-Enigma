namespace DioramaEnigma.Riddles
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
    }
}
