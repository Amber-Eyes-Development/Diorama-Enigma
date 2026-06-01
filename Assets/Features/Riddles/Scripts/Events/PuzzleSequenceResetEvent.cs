namespace DioramaEnigma.Riddles
{
    public readonly struct PuzzleSequenceResetEvent
    {
        public readonly string SequenceId;

        public PuzzleSequenceResetEvent(string sequenceId)
        {
            SequenceId = sequenceId;
        }
    }
}
