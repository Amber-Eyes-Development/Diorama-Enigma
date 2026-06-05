using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Ввод: наведение курсора. Источник события наведения для вью-реакций.
    /// </summary>
    public sealed class HoverInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary> Наведение курсора (true — наведён, false — ушёл) </summary>
        public event Action<bool> onHoverChanged;

        public void OnPointerEnter(PointerEventData eventData) => onHoverChanged?.Invoke(true);

        public void OnPointerExit(PointerEventData eventData) => onHoverChanged?.Invoke(false);
    }
}
