namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Игровое событие: объект перетащен в зону приземления
    /// </summary>
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
