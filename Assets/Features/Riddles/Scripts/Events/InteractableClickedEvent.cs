namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Игровое событие: объект сменил стейт по клику
    /// </summary>
    public readonly struct InteractableClickedEvent
    {
        public readonly string InteractableId;
        public readonly int NewStateIndex;

        public InteractableClickedEvent(string interactableId, int newStateIndex)
        {
            InteractableId = interactableId;
            NewStateIndex = newStateIndex;
        }
    }
}
