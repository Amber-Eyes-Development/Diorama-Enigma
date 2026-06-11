using Extensions.Log;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ввод: перетаскивание объёмного объекта под ортокамерой с посадкой коллайдера на сцену
    /// </summary>
    public sealed class DraggableInteractable : InteractableInput, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Группа совместимости: в какие зоны объект можно вставлять по совпадению группы (завершение шага — по совпадению шага зоны)")]
        [SerializeField] private DragGroup group;
        [Tooltip("Расстояние от камеры до плоскости драга — свободного пространства перед диорамой")]
        [SerializeField] private float dragDistance = 5f;
        [Tooltip("Длина проекции коллайдера вдоль луча камеры при установке на сцену")]
        [SerializeField] private float projectionRange = 50f;
        [Tooltip("Плавность подхвата и ведения по экранной плоскости, сек (0 — мгновенно). По оси взгляда — всегда мгновенно")]
        [SerializeField] private float followSmoothTime = 0.08f;

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

        /// <summary> Как зона принимает этот объект </summary>
        private enum ZoneAcceptance { Reject, Snap, Complete }

        private Camera mainCamera;
        private Collider cachedCollider;
        private Rigidbody body;

        private bool dragging;
        private bool finished;
        private bool wasKinematic;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private Vector2 pointerScreen;
        private Vector3 followVelocity;

        protected override void Awake()
        {
            base.Awake();
            mainCamera = Camera.main;
            cachedCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            body = GetComponent<Rigidbody>();
            wasKinematic = body.isKinematic;
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
                body.isKinematic = true;
            }

            dragging = true;
            finished = false;
            pointerScreen = eventData.position;
            followVelocity = Vector3.zero;

            LiftDepthToDragPlane();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || finished) return;

            pointerScreen = eventData.position;

            if (commitMode != DropCommitMode.OnEnter) return;

            if (TryFindZone(pointerScreen, out var zone, out var acceptance))
                CommitToZone(zone, acceptance);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging || finished) return;
            dragging = false;
            pointerScreen = eventData.position;

            if (TryFindZone(pointerScreen, out var zone, out var acceptance))
                CommitToZone(zone, acceptance);
            else
                ReleaseFree();
        }

        private void Update()
        {
            if (!dragging || finished) return;

            FollowPointer();
        }

        #region Internal

        /// <summary> Плавно вести объект под курсором по экранной плоскости, держа глубину на плоскости драга </summary>
        private void FollowPointer()
        {
            Vector3 target = DragProjection.PointerOnDragPlane(mainCamera, pointerScreen, dragDistance);
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, target, ref followVelocity, followSmoothTime);

            Vector3 fwd = mainCamera.transform.forward;
            smoothed += fwd * Vector3.Dot(target - smoothed, fwd);

            transform.position = smoothed;
        }

        /// <summary> Поднять объект на глубину плоскости драга, не меняя экранную позицию </summary>
        private void LiftDepthToDragPlane()
        {
            Vector3 fwd = mainCamera.transform.forward;
            Vector3 planePoint = mainCamera.transform.position + fwd * dragDistance;
            Vector3 pos = transform.position;

            pos += fwd * Vector3.Dot(planePoint - pos, fwd);
            transform.position = pos;
        }

        private bool TryFindZone(Vector2 screenPos, out InteractableDropZone zone, out ZoneAcceptance acceptance)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            var hits = Physics.RaycastAll(ray, projectionRange, zoneMask, QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                var candidate = hit.collider.GetComponentInParent<InteractableDropZone>();
                if (candidate == null) continue;

                var match = Classify(candidate);
                if (match == ZoneAcceptance.Reject) continue;

                zone = candidate;
                acceptance = match;
                return true;
            }

            zone = null;
            acceptance = ZoneAcceptance.Reject;
            return false;
        }

        /// <summary> Тот же шаг → завершает; иначе совместимая группа и приём чужих → только снап </summary>
        private ZoneAcceptance Classify(InteractableDropZone zone)
        {
            if (State != null && zone.Step == State) return ZoneAcceptance.Complete;
            if (zone.AcceptsForeignGroup(group)) return ZoneAcceptance.Snap;
            return ZoneAcceptance.Reject;
        }

        private void CommitToZone(InteractableDropZone zone, ZoneAcceptance acceptance)
        {
            finished = true;
            dragging = false;

            zone.Place(transform, ProjectLanding());

            if (zone.Anchor != null)
                if (body != null) body.isKinematic = true;
            else
                RestoreBody();

            ApplyCompletion(acceptance == ZoneAcceptance.Complete);
        }

        private void ReleaseFree()
        {
            finished = true;

            switch (releaseMode)
            {
                case DragReleaseMode.ReturnToStart:
                    transform.SetPositionAndRotation(startPosition, startRotation);
                    break;
                case DragReleaseMode.PlaceOnScene:
                    transform.position = ProjectLanding();
                    break;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(DragReleaseMode)}: {releaseMode}");
                    break;
            }

            RestoreBody();

            ApplyCompletion(false);
        }

        /// <summary> Привести шаг к завершённому/незавершённому (по целевому значению шага) </summary>
        private void ApplyCompletion(bool completed)
        {
            if (State == null) return;

            State.SetValue(completed ? State.CompletionState : !State.CompletionState);
        }

        private Vector3 ProjectLanding()
        {
            Ray ray = mainCamera.ScreenPointToRay(pointerScreen);
            return DragProjection.ProjectToSurface(transform, cachedCollider, ray.direction, projectionRange, surfaceMask);
        }

        private void RestoreBody()
        {
            if (body != null) body.isKinematic = wasKinematic;
        }

        #endregion
    }
}
