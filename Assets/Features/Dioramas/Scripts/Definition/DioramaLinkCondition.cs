namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Условие открытия диорамы по входящей связи <see cref="DioramaLink"/>
    /// </summary>
    public enum DioramaLinkCondition
    {
        /// <summary> Открыта, как только открыт источник (связь без доп. условий) </summary>
        SourceUnlocked = 0,
        /// <summary> Открыта, когда источник пройден целиком </summary>
        SourceCompleted = 1,
        /// <summary> Открыта, когда конкретный шаг достигает заданного состояния-триггера </summary>
        StepTrigger = 2,
    }
}
