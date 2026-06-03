namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Игровое событие: шаг последовательности провален
    /// </summary>
    public readonly struct PuzzleStepFailedEvent
    {
        public readonly string SequenceId;
        public readonly int GroupIndex;

        public PuzzleStepFailedEvent(string sequenceId, int groupIndex)
        {
            SequenceId = sequenceId;
            GroupIndex = groupIndex;
        }
    }
}
