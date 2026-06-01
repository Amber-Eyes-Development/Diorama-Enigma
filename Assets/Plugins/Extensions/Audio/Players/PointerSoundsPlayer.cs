using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

namespace Extensions.Audio
{
    /// <summary>
    /// Воспроизведение звуков кнопкой
    /// </summary>
    public class PointerSoundsPlayer : BaseAudioPlayer, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Звуки"), Space]
        [SerializeField] protected AudioResource enterSound;
        [SerializeField] protected AudioResource downSound;
        [SerializeField] protected AudioResource upSound;
        [SerializeField] protected AudioResource clickSound;

        #region IPointerHandlers
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enterSound != null) Play(enterSound);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (downSound != null) Play(downSound);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (upSound != null) Play(upSound);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickSound != null) Play(clickSound);
        }

        #endregion
    }
}