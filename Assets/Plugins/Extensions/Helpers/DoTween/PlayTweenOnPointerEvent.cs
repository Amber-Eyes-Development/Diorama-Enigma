using DG.Tweening;
using Extensions.Log;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Extensions.Helpers.DoTween
{
    /// <summary>
    /// Воспроизвести анимацию DoTween по событию указателя
    /// </summary>
    public sealed class PlayTweenOnPointerEvent : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField] private PointerTrigger trigger = PointerTrigger.Down;
        [SerializeField] private DOTweenAnimation tweener;
        [SerializeField] private bool backwards;

        private Tween tween;

        private void Start() => tween = tweener?.tween;
        
        #region PointerEvents
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (trigger == PointerTrigger.Click) Play();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (trigger == PointerTrigger.Enter) Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (trigger == PointerTrigger.Exit) Play();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (trigger == PointerTrigger.Down) Play();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (trigger == PointerTrigger.Up) Play();
        }
        
        #endregion

        private void Play()
        {
            if (tweener == null)
            {
                ServiceDebug.LogError($"Твинер не назначен, анимация не воспроизведена на объекте {name}");
                return;
            }

            if (tween == null)
            {
                ServiceDebug.LogError($"DOTweenAnimation не инициализирован на объекте {name}");
                return;
            }

            if (backwards)
            {
                tween.Goto(tween.Duration(false), true);
                tween.PlayBackwards();
            }
            else tween.Restart();
        }
    }
}