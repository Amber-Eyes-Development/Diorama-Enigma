namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Игровое событие: шаг последовательности завершён
    /// </summary>
    public readonly struct SequenceStepCompletedEvent
    {
        public readonly string SequenceId;
        public readonly int GroupIndex;

        public SequenceStepCompletedEvent(string sequenceId, int groupIndex)
        {
            SequenceId = sequenceId;
            GroupIndex = groupIndex;
        }
    }
}
