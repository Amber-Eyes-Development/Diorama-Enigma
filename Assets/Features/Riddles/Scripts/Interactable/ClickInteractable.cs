using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Ввод: клик циклически переключает значение-состояние
    /// </summary>
    /// <remarks>О «шагах» не знает — пишет <see cref="IntValue"/>, который может быть шагом-ассетом.</remarks>
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

            Apply(() => state.SetValue((state.Value + 1) % stateCount));
        }

#if UNITY_EDITOR
        public IntValue Editor_StateRef => state;
        public int Editor_StateCount => stateCount;
#endif
    }
}
