using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Хранилище float-значения
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(FloatValue),
        menuName = "Extensions/ScriptableValue/" + nameof(FloatValue)
    )]
    public class FloatValue : ScriptableValue<float> { }
}