using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Дровер <see cref="InspectorPreviewAttribute"/>: сериализованное поле без редактирования
    /// </summary>
    [CustomPropertyDrawer(typeof(InspectorPreviewAttribute))]
    
    public sealed class InspectorPreviewDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }
}