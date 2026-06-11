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
        /// <summary> Шаг, который завершается объектом с тем же шагом (из <see cref="StepReference"/> зоны; null — зона без завершения) </summary>
        public AbstractSequenceStep Step => StepRef != null ? StepRef.Step : null;

        [Tooltip("Точка фиксации: если задана, объект снапается сюда. Пусто — объект садится в спроецированную точку")]
        [SerializeField] private Transform anchor;
        [Tooltip("Снапить и поворот объекта к точке фиксации")]
        [SerializeField] private bool snapRotation = true;
        [Tooltip("Плавность присоединения")]
        [SerializeField] private float snapSmoothTime = 0.08f;
        
        [Header("Совместимость"), Space]
        [Tooltip("Принимать «чужие» объекты (с другим шагом) для снапа без завершения шага — если их группа совпадает с группой зоны")]
        [SerializeField] private bool acceptForeign;
        [Tooltip("Группа зоны: какие объекты сюда подходят (кабели↔розетки, цветы↔горшки). Совместимы при равенстве группы")]
        [SerializeField] private DragGroup group;

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

        /// <summary> Принимает ли зона «чужой» объект указанной группы (для снапа без завершения) </summary>
        public bool AcceptsForeignGroup(DragGroup other) => acceptForeign && group != null && group == other;

        /// <summary>
        /// Разместить объект в зоне: снап к точке фиксации, иначе — в <paramref name="fallbackPosition"/>
        /// </summary>
        public void Place(Transform dragged, Vector3 fallbackPosition)
        {
            if (anchor == null)
            {
                dragged.position = fallbackPosition;
                return;
            }

            if (snapRotation) 
                dragged.SetPositionAndRotation(anchor.position, anchor.rotation);
            else 
                dragged.position = anchor.position;
        }
    }
}
