using System.Collections.Generic;
using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Ввод: перетаскивание и дроп в зону выставляет значение-состояние в true
    /// </summary>
    /// <remarks>Ссылку на значение берёт из <see cref="StepReference"/> на том же объекте.</remarks>
    [RequireComponent(typeof(StepReference))]
    public sealed class DraggableInteractable : InteractableInput, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Перетаскивание"), Space]
        [Tooltip("Целевая зона. Пусто — принимается любая DropZoneObject")]
        [SerializeField] private DropZoneObject targetZone;

        private BoolValue state;
        private Camera mainCamera;
        private float dragDepth;

        private void Awake()
        {
            state = GetComponent<StepReference>().Step;
            mainCamera = Camera.main;
        }

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

#if UNITY_EDITOR
        public DropZoneObject Editor_TargetZone => targetZone;
#endif
    }
}
