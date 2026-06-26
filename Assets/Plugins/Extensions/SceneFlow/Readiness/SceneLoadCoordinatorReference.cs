using Extensions.RuntimeReferences;
using UnityEngine;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Канал рантайм-ссылки на координатор готовности целевой сцены
    /// </summary>
    /// <remarks>
    /// Координатор сцены публикует себя сюда, а контроллер сцен читает его,
    /// чтобы дождаться реальной готовности сцены и тянуть прогресс загрузки
    /// </remarks>
    [CreateAssetMenu(menuName = "Extensions/SceneFlow/" + nameof(SceneLoadCoordinatorReference))]
    public sealed class SceneLoadCoordinatorReference : RuntimeReference<SceneLoadCoordinator> { }
}
