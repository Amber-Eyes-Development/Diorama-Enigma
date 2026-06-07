using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Ввод: клик переключает булево состояние-шаг
    /// </summary>
    /// <remarks>
    /// О «шагах» не знает — пишет <see cref="BoolValue"/>, который может быть шагом-ассетом.
    /// Ссылку на значение берёт из <see cref="StepReference"/> на том же объекте.
    /// </remarks>
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
