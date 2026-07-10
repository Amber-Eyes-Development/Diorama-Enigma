using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Инспектор компонентов с выводом нередактируемых свойств
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    public sealed class MonoBehaviourReadOnlyPropertyEditor : ReadOnlyPropertyEditor { }
}
