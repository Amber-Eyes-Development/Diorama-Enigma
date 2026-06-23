namespace Extensions.Sequences
{
    /// <summary>
    /// Режим завершения композитного шага по дочерним записям (шаг + условие триггера):
    /// засчитанной считается запись, чей шаг удовлетворяет своему триггеру
    /// </summary>
    public enum CompletionMode
    {
        /// <summary> Засчитаны все дочерние записи </summary>
        All,
        /// <summary> Засчитана хотя бы одна </summary>
        Any,
        /// <summary> Засчитано не менее N </summary>
        AtLeast,
        /// <summary> Засчитано не более N </summary>
        AtMost,
        /// <summary> Засчитано ровно N </summary>
        Exactly,
        /// <summary> Не засчитана ни одна </summary>
        None,
        /// <summary> Засчитаны не все </summary>
        NotAll,
    }
}
