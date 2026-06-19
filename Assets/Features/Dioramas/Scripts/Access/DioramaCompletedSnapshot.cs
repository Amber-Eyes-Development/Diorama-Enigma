using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Снимок пройденных диорам (CSV из Id) — страховка прогресса поверх состояния шагов
    /// </summary>
    /// <remarks>
    /// На ассете включить isSaveable; isGlobal=false (прогресс отдельно для профиля)
    /// </remarks>
    [CreateAssetMenu(menuName = "Dioramas/Completed Snapshot", fileName = nameof(DioramaCompletedSnapshot))]
    public sealed class DioramaCompletedSnapshot : StringValue { }
}
