namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Игровое событие: шаг последовательности завершён
    /// </summary>
    public readonly struct PuzzleStepCompletedEvent
    {
        public readonly string SequenceId;
        public readonly int GroupIndex;

        public PuzzleStepCompletedEvent(string sequenceId, int groupIndex)
        {
            SequenceId = sequenceId;
            GroupIndex = groupIndex;
        }
    }
}
