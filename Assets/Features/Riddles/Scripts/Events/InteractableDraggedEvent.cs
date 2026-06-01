namespace DioramaEnigma.Riddles
{
    public readonly struct InteractableDraggedEvent
    {
        public readonly string InteractableId;
        public readonly string DropZoneId;

        public InteractableDraggedEvent(string interactableId, string dropZoneId)
        {
            InteractableId = interactableId;
            DropZoneId = dropZoneId;
        }
    }
}
