namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Игровое событие: последовательность сброшена к началу
    /// </summary>
    public readonly struct PuzzleSequenceResetEvent
    {
        public readonly string SequenceId;

        public PuzzleSequenceResetEvent(string sequenceId)
        {
            SequenceId = sequenceId;
        }
    }
}
