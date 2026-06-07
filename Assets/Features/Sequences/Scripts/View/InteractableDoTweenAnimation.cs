using DG.Tweening;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Реакция на событие шага запуском DOTweenAnimation
    /// </summary>
    public sealed class InteractableDoTweenAnimation : InteractableReactionBehaviour
    {
        [SerializeField] private DOTweenAnimation animation;

        protected override void React(bool silent)
        {
            if (animation == null) return;

            animation.DORestart();
            if (silent) animation.DOComplete();
        }
    }
}
