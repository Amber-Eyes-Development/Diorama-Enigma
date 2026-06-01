using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Хранилище int-значения
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(IntValue),
        menuName = "Extensions/ScriptableValue/" + nameof(IntValue)
    )]
    public class IntValue : ScriptableValue<int> { }
}