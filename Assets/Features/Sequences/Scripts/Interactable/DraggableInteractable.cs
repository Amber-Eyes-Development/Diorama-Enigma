using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ввод: перетаскивание и дроп в зону выставляет значение булева шага в true
    /// </summary>
    public sealed class DraggableInteractable : InteractableInput, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Перетаскивание"), Space]
        [Tooltip("Целевая зона. Пусто — принимается любая DropZoneObject")]
        [SerializeField] private DropZoneObject targetZone;

        private Camera mainCamera;
        private float dragDepth;

        protected override void Awake()
        {
            base.Awake();
            mainCamera = Camera.main;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, dragDepth);
            transform.position = mainCamera.ScreenToWorldPoint(screenPos);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (State == null) return;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                var zone = result.gameObject.GetComponent<DropZoneObject>();
                if (zone == null) continue;
                if (targetZone != null && zone != targetZone) continue;

                State.SetValue(true);
                return;
            }
        }

#if UNITY_EDITOR
        public DropZoneObject Editor_TargetZone => targetZone;
#endif
    }
}
