using Extensions.Generics;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Прокручивание текста по горизонтали, если он не помещается в viewport
    /// </summary>
    public sealed class HorizontalTextScroller : AbstractText
    {
        private const int INITIAL_DIRECTION = -1;
        
        [Range(10f, 50f)]
        [SerializeField] private float scrollSpeed = 25f;
        [Range(10f, 200f)]
        [SerializeField] private float returnScrollSpeed = 100f;
        [Range(0f, 10f)]
        [SerializeField] private float edgePause = 1f;
        [Range(0f, 15f)]
        [SerializeField] private float sidePadding = 8f;

        private RectTransform viewportRectTransform;
        private RectTransform textRectTransform;

        private float startX;
        private float minX;
        private float pauseTimer;
        private int direction = INITIAL_DIRECTION;
        private bool startXCaptured;

        public bool IsBlocked { get; set; }

        protected override void Awake()
        {
            base.Awake();

            if (text.enableAutoSizing) enabled = false;

            textRectTransform = text.rectTransform;
            viewportRectTransform = textRectTransform.parent as RectTransform;
        }

        private void OnEnable()
        {
            if (startXCaptured) ResetScroll();
        }

        private void Update()
        {
            if (IsBlocked) return;

            if (!startXCaptured)
            {
                if (viewportRectTransform.rect.width <= 0f) return;
                startX = textRectTransform.anchoredPosition.x;
                startXCaptured = true;
                ResetScroll();
                return;
            }

            if (!IsOverflowing())
            {
                ResetScroll();
                return;
            }

            UpdateScrollBounds();
            ScrollText();
        }

        /// <summary>
        /// Сброс положения текста
        /// </summary>
        public void ResetScroll()
        {
            pauseTimer = edgePause;
            direction = INITIAL_DIRECTION;

            Vector2 anchoredPosition = textRectTransform.anchoredPosition;
            anchoredPosition.x = startX;
            textRectTransform.anchoredPosition = anchoredPosition;
        }

        private bool IsOverflowing()
        {
            float viewportWidth = viewportRectTransform.rect.width - sidePadding * 2f;
            if (viewportWidth <= 0f) return false;

            return text.preferredWidth > viewportWidth;
        }

        private void UpdateScrollBounds()
        {
            float textWidth = text.preferredWidth;
            float viewportWidth = viewportRectTransform.rect.width - sidePadding * 2f;

            minX = startX - (textWidth - viewportWidth);
        }

        private void ScrollText()
        {
            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.unscaledDeltaTime;
                return;
            }

            float currentSpeed = direction > 0 ? returnScrollSpeed : scrollSpeed;

            Vector2 anchoredPosition = textRectTransform.anchoredPosition;
            anchoredPosition.x += direction * currentSpeed * Time.unscaledDeltaTime;

            if (anchoredPosition.x <= minX)
            {
                anchoredPosition.x = minX;
                direction = 1;
                pauseTimer = edgePause;
            }
            else if (anchoredPosition.x >= startX)
            {
                anchoredPosition.x = startX;
                direction = INITIAL_DIRECTION;
                pauseTimer = edgePause;
            }

            textRectTransform.anchoredPosition = anchoredPosition;
        }
    }
}