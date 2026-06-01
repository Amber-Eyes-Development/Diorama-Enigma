using DG.Tweening;
using Extensions.AnimationSequencer;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Визуальная реакция на наведение курсора на <see cref="InteractableObject"/>:
    /// включение/выключение объектов и воспроизведение анимаций.
    /// Размещается на том же GameObject.
    /// </summary>
    public sealed class InteractableHoverVisuals : MonoBehaviour
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

        private InteractableObject interactable;

        #region Unity Lifecycle

        private void Awake()
        {
            interactable = GetComponent<InteractableObject>();

            if (interactable == null)
                ServiceDebug.LogWarning(this, "InteractableObject не найден на этом объекте");
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.onHoverChanged += OnHoverChanged;
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.onHoverChanged -= OnHoverChanged;
        }

        #endregion

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
