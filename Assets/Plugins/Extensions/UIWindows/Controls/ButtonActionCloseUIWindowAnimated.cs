using UnityEngine;
using Extensions.Coroutines;
#if DOTWEEN
using DG.Tweening;
#endif

namespace Extensions.UIWindows
{
    public class ButtonActionCloseUIWindowAnimated : ButtonActionCloseUIWindow
    {
#if DOTWEEN
        [Header("Анимация"), Space]
        [SerializeField] protected DOTweenAnimation beforeCloseAnimation;
#endif

        public override void OnButtonClickAction()
        {
#if DOTWEEN
            if (beforeCloseAnimation)
            {
                beforeCloseAnimation.DORestart();
                CoroutineDelay.Run(this, beforeCloseAnimation.duration, base.OnButtonClickAction);
                return;
            }
#endif
            base.OnButtonClickAction();
        }
    }
}