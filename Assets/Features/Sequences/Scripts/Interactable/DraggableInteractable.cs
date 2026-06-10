using Extensions.Log;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ввод: перетаскивание объёмного объекта под ортокамерой с посадкой коллайдера на сцену
    /// </summary>
    /// <remarks>
    /// Объект поднимается на свободную плоскость перед диорамой, ведётся под курсором и проецируется
    /// вдоль луча камеры до первой поверхности. Попадание в <see cref="DropZoneObject"/> завершает шаг.
    /// Недоступный шаг не перетаскивается — фейрит <see cref="TriggerKind.InteractionRejected"/>.
    /// </remarks>
    public sealed class DraggableInteractable : InteractableInput, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Целевая зона. Пусто — принимается любая DropZoneObject")]
        [SerializeField] private DropZoneObject targetZone;
        [Tooltip("Расстояние от камеры до плоскости драга — свободного пространства перед диорамой")]
        [SerializeField] private float dragDistance = 5f;
        [Tooltip("Длина проекции коллайдера вдоль луча камеры при установке на сцену")]
        [SerializeField] private float projectionRange = 50f;
        
        [Header("Слои взаимодействия"), Space]
        [Tooltip("Слои поверхностей сцены, на которые садится объект (свои коллайдеры игнорируются автоматически)")]
        [SerializeField] private LayerMask surfaceMask = ~0;
        [Tooltip("Слои зон дропа")]
        [SerializeField] private LayerMask zoneMask = ~0;
            
        [Header("Параметры drop (завершения drag)"), Space]
        [Tooltip("Реакция на отпускание вне зоны дропа")]
        [SerializeField] private DragReleaseMode releaseMode = DragReleaseMode.PlaceOnScene;
        [Tooltip("Когда завершать шаг при попадании в зону дропа")]
        [SerializeField] private DropCommitMode commitMode = DropCommitMode.OnRelease;

        private Camera mainCamera;
        private Collider cachedCollider;
        private Rigidbody body;

        private bool dragging;
        private bool finished;
        private bool wasKinematic;
        private Vector3 startPosition;
        private Quaternion startRotation;

        protected override void Awake()
        {
            base.Awake();
            mainCamera = Camera.main;
            cachedCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            body = GetComponent<Rigidbody>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanInteract)
            {
                ReportRejectedInteraction();
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            startPosition = transform.position;
            startRotation = transform.rotation;

            if (body != null)
            {
                wasKinematic = body.isKinematic;
                body.isKinematic = true;
            }

            dragging = true;
            finished = false;

            MoveToDragPlane(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || finished) return;

            MoveToDragPlane(eventData.position);

            if (commitMode != DropCommitMode.OnEnter) return;

            // Моментальное завершение, как только объект «над» подходящей зоной (мышь ещё зажата)
            var zone = FindZoneUnderPointer(eventData.position);
            if (zone != null) CommitToZone(zone, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging || finished) return;
            dragging = false;

            var zone = FindZoneUnderPointer(eventData.position);
            if (zone != null)
            {
                CommitToZone(zone, eventData.position);
                return;
            }

            ReleaseFree(eventData.position);
        }

        #region Internal

        private void MoveToDragPlane(Vector2 screenPos) =>
            transform.position = DragProjection.PointerOnDragPlane(mainCamera, screenPos, dragDistance);

        private DropZoneObject FindZoneUnderPointer(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            var hits = Physics.RaycastAll(ray, projectionRange, zoneMask, QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                var zone = hit.collider.GetComponentInParent<DropZoneObject>();
                if (zone == null) continue;
                if (targetZone != null && zone != targetZone) continue;
                return zone;
            }

            return null;
        }

        private void CommitToZone(DropZoneObject zone, Vector2 screenPos)
        {
            finished = true;
            dragging = false;

            zone.Place(transform, ProjectLanding(screenPos));

            // С точкой фиксации объект остаётся «примонтированным» (кинематичным); иначе — обычная физика
            if (zone.Anchor != null)
            {
                if (body != null) body.isKinematic = true;
            }
            else
            {
                RestoreBody();
            }

            if (State != null) State.SetValue(true);
        }

        private void ReleaseFree(Vector2 screenPos)
        {
            finished = true;

            switch (releaseMode)
            {
                case DragReleaseMode.ReturnToStart:
                    transform.SetPositionAndRotation(startPosition, startRotation);
                    break;
                case DragReleaseMode.PlaceOnScene:
                    transform.position = ProjectLanding(screenPos);
                    break;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(DragReleaseMode)}: {releaseMode}");
                    break;
            }

            RestoreBody();
        }

        private Vector3 ProjectLanding(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            return DragProjection.ProjectToSurface(transform, cachedCollider, ray.direction, projectionRange, surfaceMask);
        }

        private void RestoreBody()
        {
            if (body != null) body.isKinematic = wasKinematic;
        }

        #endregion

#if UNITY_EDITOR
        public DropZoneObject Editor_TargetZone => targetZone;
#endif
    }
}
