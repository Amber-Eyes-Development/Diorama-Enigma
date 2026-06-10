using UnityEngine.EventSystems;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ввод: клик переключает значение булева шага
    /// </summary>
    public sealed class ClickInteractable : InteractableInput, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanInteract)
            {
                ReportRejectedInteraction();
                return;
            }

            State.SetValue(!State.Value);
        }
    }
}
