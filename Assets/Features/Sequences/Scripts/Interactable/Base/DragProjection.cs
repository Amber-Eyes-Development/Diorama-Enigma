using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Геометрия перетаскивания под ортокамерой: плоскость драга перед диорамой и проекция объекта на сцену
    /// </summary>
    public static class DragProjection
    {
        /// <summary> Точка под курсором на плоскости драга перед диорамой </summary>
        /// <remarks> В ортопроекции сдвиг по оси взгляда не меняет экранную позицию — «подъём» на плоскость незаметен </remarks>
        public static Vector3 PointerOnDragPlane(Camera cam, Vector2 screenPos, float distance)
        {
            Transform t = cam.transform;
            var plane = new Plane(-t.forward, t.position + t.forward * distance);
            Ray ray = cam.ScreenPointToRay(screenPos);

            return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : ray.GetPoint(distance);
        }

        /// <summary> Спроецировать объект вдоль <paramref name="direction"/> до упора коллайдера в поверхность </summary>
        /// <returns> Позиция <see cref="Transform"/>, при которой коллайдер касается поверхности; текущая позиция, если поверхности нет </returns>
        /// <remarks> Коллайдеры самого объекта игнорируются — <paramref name="surfaceMask"/> можно держать «всё» </remarks>
        public static Vector3 ProjectToSurface(Transform target, Collider collider, Vector3 direction,
            float maxDistance, LayerMask surfaceMask)
        {
            if (collider == null) return target.position;

            Bounds bounds = collider.bounds;
            var hits = Physics.BoxCastAll(bounds.center, bounds.extents, direction, Quaternion.identity,
                maxDistance, surfaceMask, QueryTriggerInteraction.Ignore);

            float nearest = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (IsPartOf(hit.collider.transform, target)) continue; // не задеваем сам перетаскиваемый объект
                if (hit.distance < nearest) nearest = hit.distance;
            }

            return nearest < float.PositiveInfinity
                ? target.position + direction.normalized * nearest
                : target.position;
        }

        private static bool IsPartOf(Transform candidate, Transform root) =>
            candidate == root || candidate.IsChildOf(root);
    }
}
