using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Инспектор ScriptableObject с выводом нередактируемых свойств
    /// </summary>
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    public sealed class ScriptableObjectReadOnlyPropertyEditor : ReadOnlyPropertyEditor { }
}
