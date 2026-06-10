using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Дровер <see cref="SoftRangeAttribute"/>: ползунок в [min, max] плюс числовое поле,
    /// которое принимает значения выше max и обрезает только снизу по min
    /// </summary>
    [CustomPropertyDrawer(typeof(SoftRangeAttribute))]
    internal sealed class SoftRangeDrawer : PropertyDrawer
    {
        private const float FieldWidth = 50f;
        private const float Spacing = 5f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (SoftRangeAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.Float &&
                property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "SoftRange применим только к float и int");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var controlRect = EditorGUI.PrefixLabel(position, label);
            var sliderRect = new Rect(controlRect.x, controlRect.y,
                controlRect.width - FieldWidth - Spacing, controlRect.height);
            var fieldRect = new Rect(controlRect.xMax - FieldWidth, controlRect.y, FieldWidth, controlRect.height);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (property.propertyType == SerializedPropertyType.Float)
                DrawFloat(property, attr, sliderRect, fieldRect);
            else
                DrawInt(property, attr, sliderRect, fieldRect);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static void DrawFloat(SerializedProperty property, SoftRangeAttribute attr, Rect sliderRect, Rect fieldRect)
        {
            float value = property.floatValue;

            EditorGUI.BeginChangeCheck();
            float slider = GUI.HorizontalSlider(sliderRect, Mathf.Min(value, attr.Max), attr.Min, attr.Max);
            if (EditorGUI.EndChangeCheck())
                value = slider;

            EditorGUI.BeginChangeCheck();
            float field = EditorGUI.FloatField(fieldRect, value);
            if (EditorGUI.EndChangeCheck())
                value = Mathf.Max(field, attr.Min);

            property.floatValue = value;
        }

        private static void DrawInt(SerializedProperty property, SoftRangeAttribute attr, Rect sliderRect, Rect fieldRect)
        {
            int value = property.intValue;
            int min = Mathf.RoundToInt(attr.Min);
            int max = Mathf.RoundToInt(attr.Max);

            EditorGUI.BeginChangeCheck();
            float slider = GUI.HorizontalSlider(sliderRect, Mathf.Min(value, max), min, max);
            if (EditorGUI.EndChangeCheck())
                value = Mathf.RoundToInt(slider);

            EditorGUI.BeginChangeCheck();
            int field = EditorGUI.IntField(fieldRect, value);
            if (EditorGUI.EndChangeCheck())
                value = Mathf.Max(field, min);

            property.intValue = value;
        }
    }
}
