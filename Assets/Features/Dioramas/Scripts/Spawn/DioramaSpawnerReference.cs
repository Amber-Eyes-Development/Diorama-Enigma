using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Канал рантайм-ссылки на спавнер диорам (мост сцена-UI)
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Spawner Reference", fileName = nameof(DioramaSpawnerReference))]
    public sealed class DioramaSpawnerReference : RuntimeReference<DioramaSpawner> { }
}
