using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Хранилище bool-значения
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(BoolValue),
        menuName = "Extensions/ScriptableValue/" + nameof(BoolValue)
    )]
    public class BoolValue : ScriptableValue<bool> { }
}