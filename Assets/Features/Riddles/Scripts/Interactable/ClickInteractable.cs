using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Ввод: клик циклически переключает значение-состояние.
    /// О «шагах» не знает — пишет <see cref="IntValue"/>, который может быть шагом-ассетом.
    /// </summary>
    public sealed class ClickInteractable : InteractableInput, IPointerClickHandler
    {
        [Header("Клик"), Space]
        [Tooltip("Состояние, циклически переключаемое по клику")]
        [SerializeField] private IntValue state;
        [Tooltip("Количество циклически переключаемых состояний")]
        [Min(2)]
        [SerializeField] private int stateCount = 2;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (state == null) return;

            Apply(() =>
            {
                int next = stateCount > 0 ? (state.Value + 1) % stateCount : state.Value + 1;
                state.SetValue(next);
            });
        }
    }
}
