namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Игровое событие: сработало игровое событие
    /// </summary>
    public readonly struct GameEventFiredEvent
    {
        public readonly string EventId;

        public GameEventFiredEvent(string eventId)
        {
            EventId = eventId;
        }
    }
}
