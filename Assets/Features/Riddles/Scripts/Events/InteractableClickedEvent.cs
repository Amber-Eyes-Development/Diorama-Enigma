namespace DioramaEnigma.Riddles
{
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
