using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Ресурс последовательностей: целочисленное хранилище (накапливается эффектами, тратится гейтами)
    /// </summary>
    [CreateAssetMenu(menuName = "Sequences/Resource", fileName = nameof(ResourceValue))]
    public sealed class ResourceValue : IntValue { }
}
