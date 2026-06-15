using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Дровер <see cref="EnumRangeAttribute"/>: вывод диапазона enum
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumRangeAttribute))]
    public sealed class EnumRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EnumRangeAttribute range = (EnumRangeAttribute)attribute;

            string[] names = property.enumNames;
            Array values = Enum.GetValues(fieldInfo.FieldType);

            List<string> filteredNames = new();
            List<int> filteredIndexes = new();

            for (int i = 0; i < values.Length; i++)
            {
                int enumValue = Convert.ToInt32(values.GetValue(i));

                if (enumValue < range.Min || enumValue > range.Max)
                    continue;

                filteredNames.Add(names[i]);
                filteredIndexes.Add(i);
            }

            int currentIndex = filteredIndexes.IndexOf(property.enumValueIndex);

            if (currentIndex < 0)
                currentIndex = 0;

            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, filteredNames.ToArray());

            if (filteredIndexes.Count > 0)
                property.enumValueIndex = filteredIndexes[selectedIndex];
        }
    }
}