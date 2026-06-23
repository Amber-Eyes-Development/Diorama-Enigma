using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Внутрисценовая «дверь»: по клику открывает связанную диораму, если она доступна
    /// </summary>
    /// <remarks>
    /// Размещается на объекте диорамы (напр. двери). Требует коллайдер и PhysicsRaycaster на камере —
    /// как и обычные интерактивные объекты. Переход идёт через <see cref="DioramaInstance"/> в родителе
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public sealed class DioramaPortal : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Диорама, к которой ведёт этот объект")]
        [SerializeField] private DioramaDefinition target;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (target == null) return;

            var instance = GetComponentInParent<DioramaInstance>();
            instance?.TryFocus(target);
        }
    }
}
