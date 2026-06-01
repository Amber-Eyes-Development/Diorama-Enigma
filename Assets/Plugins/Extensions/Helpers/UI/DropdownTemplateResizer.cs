using System.Collections;
using System.Collections.Generic;
using Extensions.Generics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarletBooking.UI
{
    /// <summary>
    /// Изменение размеров выпадающего списка <see cref="TMP_Dropdown"/>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Dropdown))]
    public sealed class DropdownTemplateResizer : AbstractDropdown, 
        IPointerDownHandler, IPointerClickHandler, ISubmitHandler
    {
        private const string DROPDOWN_LIST_NAME = "Dropdown List";
        private const int REFRESH_ATTEMPT_FRAMES = 30;

        [Header("Выпадающий список")]
        [SerializeField] private RectTransform content;
        
        [Header("Высота списка"), Space]
        [SerializeField] private bool stretchToScreenBottom = true;
        [SerializeField, Min(0f)] private float bottomPadding;

        [Header("Ширина списка"), Space]
        [SerializeField] private bool useFixedWidth;
        [SerializeField, Min(0f)] private float fixedWidth = 300f;
        [SerializeField] private bool stretchToScreenRight;
        [SerializeField, Min(0f)] private float rightPadding;

        private RectTransform template;
        private Canvas rootCanvas;
        private Coroutine refreshCoroutine;
        private readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        private int remainingRefreshFrames;
        private string contentPath;

        protected override void Awake()
        {
            base.Awake();
            CacheReferences();
        }

        protected override  void OnEnable()
        {
            base.OnEnable();
            ScheduleRefresh();
        }

        protected override  void OnDisable()
        {
            base.OnDisable();
            
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }

            remainingRefreshFrames = 0;
        }

        public void OnPointerDown(PointerEventData eventData) => ScheduleRefresh();

        public void OnPointerClick(PointerEventData eventData) => ScheduleRefresh();

        public void OnSubmit(BaseEventData eventData) => ScheduleRefresh();

        /// <summary>
        /// Перерасчет размеров выпадающего списка
        /// </summary>
        public void RefreshSize()
        {
            CacheReferences();

            RectTransform dropdownList = FindOpenedDropdownList();
            if (dropdownList == null) return;

            RectTransform listContent = FindOpenedContent(dropdownList);
            if (listContent == null) return;

            ResizeOpenedList(dropdownList, listContent);
        }

        private void ScheduleRefresh()
        {
            if (!isActiveAndEnabled) return;

            remainingRefreshFrames = REFRESH_ATTEMPT_FRAMES;
            if (refreshCoroutine == null)
            {
                refreshCoroutine = StartCoroutine(RefreshAfterDropdownShow());
            }
        }

        private IEnumerator RefreshAfterDropdownShow()
        {
            while (remainingRefreshFrames > 0)
            {
                yield return null;
                yield return waitForEndOfFrame;

                RefreshSize();
                remainingRefreshFrames--;
            }

            refreshCoroutine = null;
        }

        private void CacheReferences()
        {
            if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
            if (dropdown != null) template = dropdown.template;

            if (rootCanvas == null)
            {
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
            }

            if (string.IsNullOrEmpty(contentPath) && template != null && content != null)
            {
                contentPath = GetRelativePath(template, content);
            }
        }

        private RectTransform FindOpenedDropdownList()
        {
            Transform templateParent = template != null ? template.parent : null;
            RectTransform dropdownList = FindOpenedDropdownListInChildren(templateParent);
            if (dropdownList != null) return dropdownList;

            return rootCanvas != null ? FindOpenedDropdownListInChildren(rootCanvas.transform) : null;
        }

        private RectTransform FindOpenedDropdownListInChildren(Transform parent)
        {
            if (parent == null) return null;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;

                RectTransform rectTransform = child as RectTransform;
                if (rectTransform != null && child.name.StartsWith(DROPDOWN_LIST_NAME) && 
                    FindOpenedContent(rectTransform) != null)
                {
                    return rectTransform;
                }

                RectTransform nestedDropdownList = FindOpenedDropdownListInChildren(child);
                if (nestedDropdownList != null) return nestedDropdownList;
            }

            return null;
        }

        private RectTransform FindOpenedContent(RectTransform dropdownList)
        {
            if (dropdownList == null || 
                string.IsNullOrEmpty(contentPath))
            {
                return null;
            }

            return dropdownList.Find(contentPath) as RectTransform;
        }

        private void ResizeOpenedList(RectTransform dropdownList, RectTransform listContent)
        {
            Vector2 size = dropdownList.rect.size;
            bool shouldResize = false;

            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);

            if (stretchToScreenBottom)
            {
                float contentHeight = CalculateContentHeight(listContent);
                float availableHeight = CalculateHeightToScreenBottom(dropdownList);
                if (contentHeight > 0f && availableHeight > 0f)
                {
                    float viewportExtraHeight = CalculateViewportExtraHeight(dropdownList, listContent);
                    size.y = Mathf.Min(contentHeight + viewportExtraHeight, availableHeight);
                    shouldResize = true;
                }
            }

            if (useFixedWidth)
            {
                size.x = fixedWidth;
                shouldResize = true;
            }
            else if (stretchToScreenRight)
            {
                float width = CalculateWidthToScreenRight(dropdownList);
                if (width > 0f)
                {
                    size.x = width;
                    shouldResize = true;
                }
            }

            if (shouldResize) ApplySizeKeepingTopLeft(dropdownList, size);
        }

        private static float CalculateContentHeight(RectTransform listContent)
        {
            float preferredHeight = LayoutUtility.GetPreferredHeight(listContent);
            return preferredHeight > 0f ? preferredHeight : Mathf.Max(0f, listContent.rect.height);
        }

        private static float CalculateViewportExtraHeight(RectTransform dropdownList, RectTransform listContent)
        {
            RectTransform viewport = listContent.parent as RectTransform;
            if (viewport == null) return 0f;

            return Mathf.Max(0f, dropdownList.rect.height - viewport.rect.height);
        }

        private float CalculateHeightToScreenBottom(RectTransform target)
        {
            RectTransform parent = target.parent as RectTransform;
            if (parent == null) return 0f;

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Camera eventCamera = GetEventCamera(target);
            Vector2 topLeftScreen = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[1]);
            Vector2 bottomScreen = new Vector2(topLeftScreen.x, Mathf.Max(0f, bottomPadding));

            Vector2 topLeftLocal;
            Vector2 bottomLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, topLeftScreen, eventCamera, out topLeftLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, bottomScreen, eventCamera, out bottomLocal);

            return Mathf.Max(0f, topLeftLocal.y - bottomLocal.y);
        }

        private float CalculateWidthToScreenRight(RectTransform target)
        {
            RectTransform parent = target.parent as RectTransform;
            if (parent == null) return 0f;

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Camera eventCamera = GetEventCamera(target);
            Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 rightScreen = new Vector2(Screen.width - Mathf.Max(0f, rightPadding), bottomLeftScreen.y);

            Vector2 bottomLeftLocal;
            Vector2 rightLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, bottomLeftScreen, eventCamera, out bottomLeftLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, rightScreen, eventCamera, out rightLocal);

            return Mathf.Max(0f, rightLocal.x - bottomLeftLocal.x);
        }

        private static void ApplySizeKeepingTopLeft(RectTransform target, Vector2 size)
        {
            RectTransform parent = target.parent as RectTransform;
            if (parent == null)
            {
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                return;
            }

            Vector2 topLeftBefore = GetTopLeftInParent(target, parent);

            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            Vector2 topLeftAfter = GetTopLeftInParent(target, parent);
            Vector2 offset = topLeftBefore - topLeftAfter;
            target.anchoredPosition += offset;
        }

        private static Vector2 GetTopLeftInParent(RectTransform target, RectTransform parent)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            return parent.InverseTransformPoint(corners[1]);
        }

        private Camera GetEventCamera(RectTransform target)
        {
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas == null || 
                targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return targetCanvas.worldCamera;
        }

        private static string GetRelativePath(Transform root, Transform child)
        {
            List<string> pathParts = new List<string>();
            Transform current = child;

            while (current != null && current != root)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            if (current != root) return string.Empty;

            pathParts.Reverse();
            return string.Join("/", pathParts.ToArray());
        }
    }
}
