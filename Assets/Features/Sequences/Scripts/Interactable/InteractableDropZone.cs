using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Зона приземления для перетаскиваемых объектов (цель для <see cref="DraggableInteractable"/>)
    /// </summary>
    /// <remarks>
    /// Завершает шаг, когда в неё кладут объект с тем же шагом (через <see cref="StepReference"/> на зоне).
    /// Опционально принимает «чужие» объекты той же группы для снапа без завершения. Фиксирует объект в
    /// точке привязки, если задана; область зоны — коллайдер на этом объекте.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StepReference))]
    public sealed class InteractableDropZone : MonoBehaviour
    {
        /// <summary> Точка фиксации (опционально): объект снапается сюда при попадании в зону </summary>
        public Transform Anchor => anchor;
        /// <summary> Снапить и поворот объекта к точке фиксации </summary>
        public bool SnapRotation => snapRotation;
        /// <summary> Плавность присоединения, сек (0 — мгновенно) </summary>
        public float SnapSmoothTime => snapSmoothTime;
        /// <summary> Засчитывать ли физический контакт объекта с зоной как дроп (объект сам закатился) </summary>
        public bool AcceptsPhysicsDrop => acceptPhysicsDrop;
        /// <summary> Шаг, который завершается объектом с тем же шагом (из <see cref="StepReference"/> зоны; null — зона без завершения) </summary>
        public AbstractSequenceStep Step => StepRef != null ? StepRef.Step : null;

        [Tooltip("Точка фиксации: если задана, объект снапается сюда. Пусто — объект садится в спроецированную точку")]
        [SerializeField] private Transform anchor;
        [Tooltip("Снапить и поворот объекта к точке фиксации")]
        [SerializeField] private bool snapRotation = true;
        [Tooltip("Плавность присоединения, сек (0 — мгновенно)")]
        [SerializeField] private float snapSmoothTime = 0.08f;

        [Header("Совместимость"), Space]
        [Tooltip("Принимать «чужие» объекты (с другим шагом) для снапа без завершения шага — если их группа совпадает с группой зоны")]
        [SerializeField] private bool acceptForeign = false;
        [Tooltip("Группа зоны: какие объекты сюда подходят (кабели↔розетки, цветы↔горшки). Совместимы при равенстве группы")]
        [SerializeField] private DragGroup group;
        [Tooltip("Засчитывать дроп при физическом касании объекта с зоной (требуется триггер-коллайдер)")]
        [SerializeField] private bool acceptPhysicsDrop = false;

        private StepReference stepRefCache;
        private bool stepRefResolved;

        private StepReference StepRef
        {
            get
            {
                if (!stepRefResolved)
                {
                    stepRefCache = GetComponent<StepReference>();
                    stepRefResolved = true;
                }
                return stepRefCache;
            }
        }

        private void Awake()
        {
            var zoneCollider = GetComponentInChildren<Collider>();
            if (zoneCollider == null)
                ServiceDebug.LogWarning(this, "Нет коллайдера — зона дропа не сможет принимать объекты");
            else if (!zoneCollider.isTrigger)
                ServiceDebug.LogWarning(this, "Коллайдер зоны дропа должен быть триггером (Is Trigger)");
        }

        /// <summary> Принимает ли зона «чужой» объект указанной группы (для снапа без завершения) </summary>
        public bool AcceptsForeignGroup(DragGroup other) => acceptForeign && group != null && group == other;

        /// <summary> Целевая поза размещения: точка фиксации, иначе — <paramref name="fallbackPosition"/> </summary>
        public void GetSnapTarget(Vector3 fallbackPosition, out Vector3 position, out Quaternion rotation, out bool applyRotation)
        {
            if (anchor != null)
            {
                position = anchor.position;
                rotation = anchor.rotation;
                applyRotation = snapRotation;
            }
            else
            {
                position = fallbackPosition;
                rotation = Quaternion.identity;
                applyRotation = false;
            }
        }

        /// <summary> Мгновенно разместить объект в зоне (для восстановления из сейва) </summary>
        public void Place(Transform dragged, Vector3 fallbackPosition)
        {
            GetSnapTarget(fallbackPosition, out var position, out var rotation, out bool applyRotation);

            if (applyRotation) dragged.SetPositionAndRotation(position, rotation);
            else dragged.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!acceptPhysicsDrop) return;

            var draggable = other.GetComponentInParent<DraggableInteractable>();
            if (draggable != null) draggable.TryPhysicsCommit(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!acceptPhysicsDrop) return;

            var draggable = other.GetComponentInParent<DraggableInteractable>();
            if (draggable != null) draggable.OnPhysicsExitedZone(this);
        }
    }
}
