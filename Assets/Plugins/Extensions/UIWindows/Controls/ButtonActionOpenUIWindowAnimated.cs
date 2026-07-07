using UnityEngine;
using Extensions.Coroutines;
#if DOTWEEN
using DG.Tweening;
#endif

namespace Extensions.UIWindows
{
    public class ButtonActionOpenUIWindowAnimated : ButtonActionOpenUIWindow
    {
#if DOTWEEN
        [Header("Анимация"), Space]
        [SerializeField] protected DOTweenAnimation beforeOpenAnimation;
        [SerializeField] protected bool realtime = true;
#endif

        public override void OnButtonClickAction()
        {
#if DOTWEEN
            if (openMode == UIWindowOpenMode.Pop && beforeOpenAnimation)
            {
                beforeOpenAnimation.DORestart();
                CoroutineDelay.Run(this, beforeOpenAnimation.duration, base.OnButtonClickAction, realtime);
                return;
            }
#endif
            base.OnButtonClickAction();
        }
    }
}