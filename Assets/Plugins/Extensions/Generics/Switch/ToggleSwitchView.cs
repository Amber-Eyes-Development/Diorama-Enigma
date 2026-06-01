using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Extensions.Generics
{
    /// <summary>
    /// Визуальный контроллер мобильного свитча
    /// </summary>
    public class ToggleSwitchView : AbstractToggle
    {
        [Header("Графика"), Space]
        [SerializeField] protected RectTransform handle;
        [SerializeField] protected Image background;

        [Header("Точки позиции (пустые RectTransform)"), Space]
        [SerializeField] protected RectTransform onPoint;
        [SerializeField] protected RectTransform offPoint;

        [Header("Цвета"), Space]
        [SerializeField] protected Color offColor = new Color(0.78f, 0.78f, 0.78f);
        [SerializeField] protected Color onColor  = new Color(0.20f, 0.78f, 0.35f);

        [Header("Анимация"), Space]
        [SerializeField] protected float duration = 0.3f;
        [SerializeField] protected Ease easeType = Ease.OutBack;


        protected override void Awake()
        {
            base.Awake();
            
            toggle.targetGraphic = null;
            toggle.graphic = null;
            toggle.transition = Selectable.Transition.None;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            SetState(toggle.isOn, false);
        }

        /// <summary>
        /// Обновить визуальное состояние свитча
        /// </summary>
        /// <param name="state">Состояние активации свитча</param>
        /// <param name="animate">Воспроизводить анимацию или применить мгновенно</param>
        public void SetState(bool state, bool animate)
        {
            Vector2 targetPos = state ? offPoint.anchoredPosition : onPoint.anchoredPosition;
            Color targetColor = state ? onColor : offColor;

            if (animate)
            {
                AnimateHandle(targetPos);
                AnimateBackground(targetColor);
            }
            else
            {
                handle.anchoredPosition = targetPos;
                background.color = targetColor;
                handle.localScale = Vector3.one;
            }
        }
        
        public override void OnToggled(bool state) => SetState(state, true);
        
        protected virtual void AnimateHandle(Vector2 targetPos)
        {
            handle.DOAnchorPos(targetPos, duration).SetEase(easeType);

            handle.DOScale(new Vector3(1.2f, 0.85f, 1f), duration * 0.3f)
                  .SetEase(Ease.OutQuad)
                  .OnComplete(() =>
                      handle.DOScale(Vector3.one, duration * 0.4f).SetEase(Ease.OutBack)
                  );
        }

        protected virtual void AnimateBackground(Color targetColor)
        {
            background.DOColor(targetColor, duration);
        }
    }
}