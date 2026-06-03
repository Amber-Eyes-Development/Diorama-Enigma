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
        [SerializeField] private GameObject[] objectsToEnableOnEnter;
        [SerializeField] private GameObject[] objectsToDisableOnEnter;
        [SerializeField] private DOTweenAnimation enterAnimation;
        [SerializeField] private AnimationSequencer enterSequencer;

        [Header("При уходе"), Space]
        [SerializeField] private GameObject[] objectsToEnableOnExit;
        [SerializeField] private GameObject[] objectsToDisableOnExit;
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
            SetActive(objectsToEnableOnEnter, true);
            SetActive(objectsToDisableOnEnter, false);
            enterAnimation?.DORestart();
            enterSequencer?.Play();
        }

        private void OnHoverExit()
        {
            SetActive(objectsToEnableOnExit, true);
            SetActive(objectsToDisableOnExit, false);
            exitAnimation?.DORestart();
            exitSequencer?.Play();
        }

        private static void SetActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (var obj in objects)
                if (obj != null) obj.SetActive(active);
        }

        #endregion
    }
}
