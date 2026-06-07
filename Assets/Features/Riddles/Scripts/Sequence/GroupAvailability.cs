namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Когда шаги группы становятся доступны для изменения (ввода)
    /// </summary>
    public enum GroupAvailability
    {
        /// <summary> Только после завершения предыдущих групп (порядок) </summary>
        AfterPreviousGroups = 0,

        /// <summary> С самого старта — можно работать в любой момент, вне порядка </summary>
        Always = 1,
    }
}
