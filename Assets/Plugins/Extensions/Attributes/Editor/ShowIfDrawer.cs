using System;
using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Дровер <see cref="ShowIfAttribute"/>: рисует поле только когда поле-условие равно заданному значению,
    /// иначе схлопывает его без межполевого отступа
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    internal sealed class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsShown(property)) EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return IsShown(property)
                ? EditorGUI.GetPropertyHeight(property, label, true)
                : -EditorGUIUtility.standardVerticalSpacing;
        }

        private bool IsShown(SerializedProperty property)
        {
            var attr = (ShowIfAttribute)attribute;
            SerializedProperty condition = FindSibling(property, attr.FieldName);
            if (condition == null) return true; 

            switch (condition.propertyType)
            {
                case SerializedPropertyType.Enum: return condition.enumValueIndex == Convert.ToInt32(attr.Value);
                case SerializedPropertyType.Boolean: return condition.boolValue.Equals(attr.Value);
                case SerializedPropertyType.Integer: return condition.intValue == Convert.ToInt32(attr.Value);
                default: return true;
            }
        }

        /// <summary> Поле-условие в той же вложенности (учёт пути для вложенных типов/массивов) </summary>
        private static SerializedProperty FindSibling(SerializedProperty property, string fieldName)
        {
            string path = property.propertyPath;
            int dot = path.LastIndexOf('.');
            return dot < 0
                ? property.serializedObject.FindProperty(fieldName)
                : property.serializedObject.FindProperty($"{path[..dot]}.{fieldName}");
        }
    }
}
