using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Хранилище string-значения
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(StringValue),
        menuName = "Extensions/ScriptableValue/" + nameof(StringValue)
    )]
    public class StringValue : ScriptableValue<string> { }
}