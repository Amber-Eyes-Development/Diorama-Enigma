namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Когда шаги группы становятся доступны для изменения (ввода)
    /// </summary>
    public enum GroupAvailability
    {
        /// <summary> Только после завершения предыдущих групп (порядок) </summary>
        AfterPreviousGroups = 0,
        /// <summary> С самого старта — можно работать в любой момент, вне порядка </summary>
        /// <remarks> В гейтировании по группам не участвуют, поочередность в последовательности не важна </remarks>
        Always = 1,
    }
}
