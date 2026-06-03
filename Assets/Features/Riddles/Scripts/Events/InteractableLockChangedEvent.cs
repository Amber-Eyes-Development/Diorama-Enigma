namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Игровое событие: изменена блокировка объекта
    /// </summary>
    public readonly struct InteractableLockChangedEvent
    {
        public readonly string InteractableId;
        public readonly bool IsLocked;

        public InteractableLockChangedEvent(string interactableId, bool isLocked)
        {
            InteractableId = interactableId;
            IsLocked = isLocked;
        }
    }
}
