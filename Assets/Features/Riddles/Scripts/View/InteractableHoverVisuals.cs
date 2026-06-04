using DG.Tweening;
using Extensions.AnimationSequencer;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Визуальная реакция на наведение курсора
    /// </summary>
    public sealed class InteractableHoverVisuals : InteractableViewBehaviour
    {
        [Header("При наведении"), Space]
        [SerializeField] private GameObjectActivation[] objectsOnEnter;
        [SerializeField] private DOTweenAnimation enterAnimation;
        [SerializeField] private AnimationSequencer enterSequencer;

        [Header("При уходе"), Space]
        [SerializeField] private GameObjectActivation[] objectsOnExit;
        [SerializeField] private DOTweenAnimation exitAnimation;
        [SerializeField] private AnimationSequencer exitSequencer;

        protected override void Subscribe() => Interactable.onHoverChanged += OnHoverChanged;

        protected override void Unsubscribe() => Interactable.onHoverChanged -= OnHoverChanged;

        #region Internal

        private void OnHoverChanged(bool isHovered)
        {
            if (isHovered) OnHoverEnter();
            else OnHoverExit();
        }

        private void OnHoverEnter()
        {
            objectsOnEnter.Apply();
            enterAnimation?.DORestart();
            enterSequencer?.Play();
        }

        private void OnHoverExit()
        {
            objectsOnExit.Apply();
            exitAnimation?.DORestart();
            exitSequencer?.Play();
        }

        #endregion
    }
}
