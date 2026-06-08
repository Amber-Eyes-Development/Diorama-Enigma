using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ввод: клик переключает значение булева шага
    /// </summary>
    [RequireComponent(typeof(StepReference))]
    public sealed class ClickInteractable : InteractableInput, IPointerClickHandler
    {
        private SequenceStep state;

        private void Awake() => state = GetComponent<StepReference>().Step as SequenceStep;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (state == null) return;

            Apply(() => state.SetValue(!state.Value));
        }
    }
}
