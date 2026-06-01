using UnityEngine;
using UnityEngine.EventSystems;

namespace Extensions.Helpers
{
    /// <summary>
    /// Подгон элемента/экрана UI под safe area устройства
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : UIBehaviour
    {
        private RectTransform target;

        protected override void Awake()
        {
            if (target == null) target = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        protected override void OnRectTransformDimensionsChange() => ApplySafeArea();

        private void ApplySafeArea()
        {
            if (target == null) return;

            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;

            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}