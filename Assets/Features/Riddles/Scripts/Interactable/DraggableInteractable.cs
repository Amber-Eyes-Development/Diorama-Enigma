using System.Collections.Generic;
using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Ввод: перетаскивание и дроп в зону выставляет значение-состояние в true.
    /// Целевая зона — прямая ссылка на <see cref="DropZoneObject"/> (без идентификаторов).
    /// </summary>
    public sealed class DraggableInteractable : InteractableInput, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Перетаскивание"), Space]
        [Tooltip("Состояние, выставляемое в true при дропе в нужную зону")]
        [SerializeField] private BoolValue state;
        [Tooltip("Целевая зона. Пусто — принимается любая DropZoneObject")]
        [SerializeField] private DropZoneObject targetZone;

        private Camera mainCamera;
        private float dragDepth;

        private void Awake() => mainCamera = Camera.main;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsBusy) return;

            if (mainCamera == null) mainCamera = Camera.main;
            dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsBusy) return;

            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, dragDepth);
            transform.position = mainCamera.ScreenToWorldPoint(screenPos);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsBusy || state == null) return;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                var zone = result.gameObject.GetComponent<DropZoneObject>();
                if (zone == null) continue;
                if (targetZone != null && zone != targetZone) continue;

                Apply(() => state.SetValue(true));
                return;
            }
        }
    }
}
