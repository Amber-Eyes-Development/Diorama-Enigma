namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Игровое событие: шаг последовательности провален
    /// </summary>
    public readonly struct SequenceStepFailedEvent
    {
        public readonly string SequenceId;
        public readonly int GroupIndex;

        public SequenceStepFailedEvent(string sequenceId, int groupIndex)
        {
            SequenceId = sequenceId;
            GroupIndex = groupIndex;
        }
    }
}
