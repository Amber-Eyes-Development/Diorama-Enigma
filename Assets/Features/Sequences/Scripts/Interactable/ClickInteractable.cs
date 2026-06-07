using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ввод: клик переключает булево состояние-шаг
    /// </summary>
    [RequireComponent(typeof(StepReference))]
    public sealed class ClickInteractable : InteractableInput, IPointerClickHandler
    {
        private BoolValue state;

        private void Awake() => state = GetComponent<StepReference>().Step;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (state == null) return;

            Apply(() => state.SetValue(!state.Value));
        }
    }
}
