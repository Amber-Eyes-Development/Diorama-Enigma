using DG.Tweening;
using Extensions.RuntimeReferences;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DioramaEnigma.Inventory
{
    /// <summary> Всплывающая плашка с текстом предмета: следует за курсором, появляется/исчезает анимацией </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class TooltipPresenter : MonoBehaviour, ITooltipView
    {
        [Tooltip("Канал, в который публикуется эта плашка")]
        [SerializeField] private TooltipViewReference reference;
        [SerializeField] private TMP_Text label;

        [Header("Слежение за курсором")]
        [Tooltip("Плавно следовать за курсором; выключено — плашка остаётся на позиции, заданной в префабе")]
        [SerializeField] private bool followCursor = true;
        [SerializeField] private float followSpeed = 15f;

        [Header("Анимации")]
        [SerializeField] private DOTweenAnimation[] appearAnimations;
        [SerializeField] private DOTweenAnimation[] disappearAnimations;

        private RectTransform rect;
        private RectTransform canvasRect;
        private Camera uiCamera;
        private bool isShown;

        private void Awake()
        {
            rect = (RectTransform)transform;

            var canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.transform as RectTransform : rect.parent as RectTransform;
            uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            SetupGroup(appearAnimations);
            SetupGroup(disappearAnimations);

            CreateGroup(appearAnimations);
            CompleteGroup(appearAnimations);
            CreateGroup(disappearAnimations);
            CompleteGroup(disappearAnimations);
            isShown = false;
        }

        private void OnEnable() => reference?.Set(this);

        private void OnDisable()
        {
            if (reference != null && ReferenceEquals(reference.Current, this)) reference.Clear();
        }

        private void Update()
        {
            if (isShown && followCursor) FollowCursor(instant: false);
        }

        /// <inheritdoc/>
        public void Show(string text)
        {
            if (label != null) label.text = text;

            isShown = true;
            if (followCursor) FollowCursor(instant: true);

            CompleteIfPlaying(disappearAnimations);
            PlayGroup(appearAnimations);
        }

        /// <inheritdoc/>
        public void Hide()
        {
            isShown = false;

            CompleteIfPlaying(appearAnimations);
            PlayGroup(disappearAnimations);
        }

        private static void CreateGroup(DOTweenAnimation[] group)
        {
            if (group == null) return;

            foreach (var animation in group)
                if (animation != null) EnsureTween(animation);
        }

        /// <summary> Мгновенно, без анимации, прогнать твины группы в их конечное значение </summary>
        private static void CompleteGroup(DOTweenAnimation[] group)
        {
            if (group == null) return;

            foreach (var animation in group)
                if (animation != null) animation.DOComplete();
        }

        private void FollowCursor(bool instant)
        {
            if (canvasRect == null || Mouse.current == null) return;

            Vector2 screenPoint = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out var localPoint))
                return;

            rect.anchoredPosition = instant
                ? localPoint
                : Vector2.Lerp(rect.anchoredPosition, localPoint, Time.unscaledDeltaTime * followSpeed);
        }

        private static void PlayGroup(DOTweenAnimation[] group)
        {
            if (group == null) return;

            foreach (var animation in group)
            {
                if (animation == null) continue;

                EnsureTween(animation);
                animation.DORestart();
            }
        }

        private static void CompleteIfPlaying(DOTweenAnimation[] group)
        {
            if (group == null) return;

            foreach (var animation in group)
                if (animation != null && animation.tween != null && animation.tween.IsPlaying())
                    animation.DOComplete();
        }

        private static void SetupGroup(DOTweenAnimation[] group)
        {
            if (group == null) return;

            foreach (var animation in group)
            {
                if (animation == null) continue;

                animation.autoGenerate = false;
                animation.autoPlay = false;
                animation.autoKill = false;
            }
        }

        /// <summary> Создать твин, если его ещё нет (вью сама управляет проигрыванием) </summary>
        private static void EnsureTween(DOTweenAnimation animation)
        {
            if (animation.tween == null)
                animation.CreateTween(regenerateIfExists: false, andPlay: false);
        }
    }
}
