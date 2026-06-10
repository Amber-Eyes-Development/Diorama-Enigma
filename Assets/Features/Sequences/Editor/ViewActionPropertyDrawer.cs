using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Дровер записи действия (<see cref="ViewAction"/> и наследники)
    /// </summary>
    [CustomPropertyDrawer(typeof(ViewAction), true)]
    internal sealed class ViewActionPropertyDrawer : PropertyDrawer
    {
        private const string TriggerField = "trigger";
        private const string DelayField = "delay";
        private const string ReversibleField = "reversible";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;

                var trigger = property.FindPropertyRelative(TriggerField);
                var delay = property.FindPropertyRelative(DelayField);
                var reversible = property.FindPropertyRelative(ReversibleField);

                y = DrawTriggerAndReversible(position, y, trigger, reversible);
                y = DrawRow(position, y, delay);

                foreach (var child in OwnChildren(property))
                    y = DrawRow(position, y, child);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;

            float spacing = EditorGUIUtility.standardVerticalSpacing;

            height += EditorGUIUtility.singleLineHeight + spacing;

            var delay = property.FindPropertyRelative(DelayField);
            height += EditorGUI.GetPropertyHeight(delay, true) + spacing;

            foreach (var child in OwnChildren(property))
                height += EditorGUI.GetPropertyHeight(child, true) + spacing;

            return height;
        }

        /// <summary> Триггер и обратимость бок о бок в одну строку </summary>
        private static float DrawTriggerAndReversible(Rect position, float y,
            SerializedProperty trigger, SerializedProperty reversible)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            var rowRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, lineH));

            int indent = EditorGUI.indentLevel;
            float prevLabel = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0; 

            const float spacing = 12f;
            const float reversibleWidth = 90f;
            var rightRect = new Rect(rowRect.xMax - reversibleWidth, rowRect.y, reversibleWidth, lineH);
            var leftRect = new Rect(rowRect.x, rowRect.y, rowRect.width - reversibleWidth - spacing, lineH);

            EditorGUIUtility.labelWidth = 50f;
            EditorGUI.PropertyField(leftRect, trigger, new GUIContent(trigger.displayName));

            EditorGUIUtility.labelWidth = 70f;
            EditorGUI.PropertyField(rightRect, reversible, new GUIContent(reversible.displayName));

            EditorGUIUtility.labelWidth = prevLabel;
            EditorGUI.indentLevel = indent;

            return y + lineH + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary> Поле во всю ширину отдельной строкой </summary>
        private static float DrawRow(Rect position, float y, SerializedProperty property)
        {
            float h = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), property, true);
            return y + h + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary> Поля частной реализации — все, кроме базовых trigger/delay/reversible </summary>
        private static IEnumerable<SerializedProperty> OwnChildren(SerializedProperty property)
        {
            var child = property.Copy();
            var end = child.GetEndProperty();

            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                if (child.name is TriggerField or DelayField or ReversibleField) continue;
                yield return child.Copy();
            }
        }
    }
}
