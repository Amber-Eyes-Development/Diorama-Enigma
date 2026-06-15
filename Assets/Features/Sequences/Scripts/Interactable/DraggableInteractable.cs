using System;
using Extensions.Data;
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
        private const string PLACEMENT_KEY_POSTFIX = "_drag_placement";
        
        private const float SNAP_POS_EPSILON_SQR = 1e-6f;
        private const float SNAP_ANGLE_EPSILON = 0.1f;

        #region Параметры
        
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
        [Tooltip("Сохранять точную позицию объекта в зоне между сессиями (для любой зоны со снапом). Выкл — позиция не сохраняется")]
        [SerializeField] private bool savePlacement;

        #endregion
        
        private enum ZoneAcceptance { Reject, Snap, Complete }

        [Serializable]
        private struct PlacementSave
        {
            public bool snapped;
            public Vector3 position;
            public Quaternion rotation;
        }
        
        #region Переменные

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

        private bool snapping;
        private Vector3 snapTargetPos;
        private Quaternion snapTargetRot;
        private bool snapApplyRot;
        private float snapTime;
        private Vector3 snapVelocity;

        private InteractableDropZone committedZone;
        
        #endregion

        private string PlacementKey => State != null ? State.Id + PLACEMENT_KEY_POSTFIX : null;

        protected override void Awake()
        {
            base.Awake();
            mainCamera = Camera.main;
            cachedCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            body = GetComponent<Rigidbody>();
            if (body != null) wasKinematic = body.isKinematic;
        }

        private void Start() => RestoreIfCommitted();

        #region Drag/Drop

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (State == null) return;
            if (!State.CanChangeValue)
            {
                State.NotifyInteractionRejected();
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            startPosition = transform.position;
            startRotation = transform.rotation;

            if (body != null) body.isKinematic = true;

            dragging = true;
            finished = false;
            snapping = false;
            committedZone = null;
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
                CommitToZone(zone, acceptance, ProjectLanding());
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging || finished) return;
            dragging = false;
            pointerScreen = eventData.position;

            if (TryFindZone(pointerScreen, out var zone, out var acceptance))
                CommitToZone(zone, acceptance, ProjectLanding());
            else
                ReleaseFree();
        }
        
        #endregion

        /// <summary> Дроп по физическому контакту: объект сам закатился в зону (вызывает зона) </summary>
        public void TryPhysicsCommit(InteractableDropZone zone)
        {
            if (dragging || snapping) return;

            var acceptance = Classify(zone);
            if (acceptance == ZoneAcceptance.Reject) return;

            CommitToZone(zone, acceptance, transform.position);
        }

        private void Update()
        {
            if (dragging && !finished) FollowPointer();
            else if (snapping) SnapStep();
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

        /// <summary> Плавно довести объект до точки фиксации (как подхват), затем завершить снап </summary>
        private void SnapStep()
        {
            transform.position = Vector3.SmoothDamp(transform.position, snapTargetPos, ref snapVelocity, snapTime);

            if (snapApplyRot)
            {
                float t = snapTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / snapTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, snapTargetRot, t);
            }

            bool posDone = (transform.position - snapTargetPos).sqrMagnitude < SNAP_POS_EPSILON_SQR;
            bool rotDone = !snapApplyRot || Quaternion.Angle(transform.rotation, snapTargetRot) < SNAP_ANGLE_EPSILON;
            if (!posDone || !rotDone) return;

            transform.position = snapTargetPos;
            if (snapApplyRot) transform.rotation = snapTargetRot;
            snapping = false;
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

        private void CommitToZone(InteractableDropZone zone, ZoneAcceptance acceptance, Vector3 fallback)
        {
            finished = true;
            dragging = false;
            committedZone = zone;

            if (zone.Anchor != null)
            {
                if (body != null) body.isKinematic = true;
                StartSnap(zone);
                SavePlacement(true, snapTargetPos, snapApplyRot ? snapTargetRot : transform.rotation);
            }
            else
            {
                snapping = false;
                zone.Place(transform, fallback);
                RestoreBody();
                SavePlacement(false, default, default);
            }

            ApplyCompletion(acceptance == ZoneAcceptance.Complete);
        }

        private void StartSnap(InteractableDropZone zone)
        {
            zone.GetSnapTarget(transform.position, out snapTargetPos, out snapTargetRot, out snapApplyRot);
            snapTime = zone.SnapSmoothTime;
            snapVelocity = Vector3.zero;
            snapping = true;
        }

        private void ReleaseFree()
        {
            finished = true;
            snapping = false;
            committedZone = null;

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
            SavePlacement(false, default, default);

            ApplyCompletion(false);
        }

        /// <summary> Физический выход из зоны (вызывает зона): покинул свою завершающую зону → откат шага </summary>
        public void OnPhysicsExitedZone(InteractableDropZone zone)
        {
            if (dragging || snapping) return;
            if (committedZone != zone) return;

            committedZone = null;

            if (State != null && zone.Step == State)
                RevertCompletion();

            SavePlacement(false, default, default);
        }

        /// <summary> Системный откат завершённости (минуя кулдаун), если шаг разблокирован и обратим </summary>
        private void RevertCompletion()
        {
            if (State == null || !State.IsActive || State.Irreversible) return;

            State.ForceValue(!State.CompletionState);
        }

        /// <summary> Восстановить позу снапа из сейва (любая зона со снапом); фолбэк — усадить завершённый шаг в свою зону </summary>
        private void RestoreIfCommitted()
        {
            if (savePlacement && PlacementKey != null)
            {
                var placement = JsonSaveLoad.Load(PlacementKey, default(PlacementSave));
                if (placement.snapped)
                {
                    ApplySnappedPose(placement.position, placement.rotation);
                    return;
                }
            }

            if (State == null || !State.IsCompleted) return;

            var zone = FindZoneForStep();
            if (zone == null || zone.Anchor == null) return;

            ApplySnappedPose(zone.Anchor.position, zone.SnapRotation ? zone.Anchor.rotation : transform.rotation);
        }

        private void ApplySnappedPose(Vector3 position, Quaternion rotation)
        {
            if (body != null) body.isKinematic = true;
            transform.SetPositionAndRotation(position, rotation);
            finished = true;
        }

        private void SavePlacement(bool snapped, Vector3 position, Quaternion rotation)
        {
            if (!savePlacement || !Application.isPlaying || PlacementKey == null) return;

            JsonSaveLoad.Save(new PlacementSave { snapped = snapped, position = position, rotation = rotation }, PlacementKey);
        }

        private InteractableDropZone FindZoneForStep()
        {
            foreach (var zone in FindObjectsByType<InteractableDropZone>(FindObjectsSortMode.None))
                if (zone.Step == State) return zone;

            return null;
        }

        /// <summary> Привести шаг к завершённому/незавершённому (по целевому значению шага) </summary>
        private void ApplyCompletion(bool completed)
        {
            if (State == null) return;

            State.SetValue(completed ? State.CompletionState : !State.CompletionState, notify: false);
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
