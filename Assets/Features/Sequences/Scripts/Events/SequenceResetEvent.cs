namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Игровое событие: последовательность сброшена к началу
    /// </summary>
    public readonly struct SequenceResetEvent
    {
        public readonly string SequenceId;

        public SequenceResetEvent(string sequenceId)
        {
            SequenceId = sequenceId;
        }
    }
}
