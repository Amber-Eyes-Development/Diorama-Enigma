using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Зона приземления для перетаскиваемых объектов (цель для <see cref="DraggableInteractable"/>)
    /// </summary>
    /// <remarks> Опционально фиксирует объект в заданной точке; область зоны — коллайдер на этом объекте </remarks>
    public sealed class DropZoneObject : MonoBehaviour
    {
        /// <summary> Точка фиксации (опционально): объект снапается сюда при попадании в зону </summary>
        public Transform Anchor => anchor;

        [Tooltip("Точка фиксации: если задана, объект снапается сюда. Пусто — объект садится в спроецированную точку")]
        [SerializeField] private Transform anchor;
        [Tooltip("Снапить и поворот объекта к точке фиксации")]
        [SerializeField] private bool snapRotation = true;

        /// <summary> Разместить объект в зоне: снап к точке фиксации, иначе — в <paramref name="fallbackPosition"/> </summary>
        public void Place(Transform dragged, Vector3 fallbackPosition)
        {
            if (anchor == null)
            {
                dragged.position = fallbackPosition;
                return;
            }

            if (snapRotation) dragged.SetPositionAndRotation(anchor.position, anchor.rotation);
            else dragged.position = anchor.position;
        }
    }
}
