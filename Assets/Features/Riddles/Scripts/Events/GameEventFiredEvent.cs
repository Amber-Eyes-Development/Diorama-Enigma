namespace DioramaEnigma.Riddles
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
