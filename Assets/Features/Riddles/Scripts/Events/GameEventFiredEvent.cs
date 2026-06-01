namespace DioramaEnigma.Riddles
{
    public readonly struct GameEventFiredEvent
    {
        public readonly string EventId;

        public GameEventFiredEvent(string eventId)
        {
            EventId = eventId;
        }
    }
}
