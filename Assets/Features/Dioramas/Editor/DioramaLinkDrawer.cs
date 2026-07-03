using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Dioramas.Editor
{
    /// <summary>
    /// Дровер связи <see cref="DioramaLink"/>
    /// </summary>
    /// <remarks>
    /// Поля шага показываются только для условия <see cref="DioramaLinkCondition.StepTrigger"/>.
    /// Поля рисуются через PropertyField, поэтому их собственные дроверы (напр. EnumRange у триггера) работают штатно
    /// </remarks>
    [CustomPropertyDrawer(typeof(DioramaLink))]
    internal sealed class DioramaLinkDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;

                y = DrawField(position, y, property.FindPropertyRelative("source"));

                var conditionProp = property.FindPropertyRelative("condition");
                y = DrawField(position, y, conditionProp);

                if (IsStepTrigger(conditionProp))
                {
                    y = DrawField(position, y, property.FindPropertyRelative("step"));
                    DrawField(position, y, property.FindPropertyRelative("stepTrigger"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;

            height += FieldHeight(property, "source");

            var conditionProp = property.FindPropertyRelative("condition");
            height += FieldHeight(property, "condition");

            if (IsStepTrigger(conditionProp))
            {
                height += FieldHeight(property, "step");
                height += FieldHeight(property, "stepTrigger");
            }

            return height;
        }

        private static float DrawField(Rect position, float y, SerializedProperty prop)
        {
            float h = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), prop, true);
            return y + h + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float FieldHeight(SerializedProperty property, string relativeName)
        {
            var prop = property.FindPropertyRelative(relativeName);
            return EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        // enum объявлен последовательно от 0, поэтому enumValueIndex совпадает со значением
        private static bool IsStepTrigger(SerializedProperty conditionProp) =>
            conditionProp != null && conditionProp.enumValueIndex == (int)DioramaLinkCondition.StepTrigger;
    }
}
