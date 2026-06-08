using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Дровер поля-гейта (<see cref="StepGate"/>, SerializeReference): явный выпадающий выбор
    /// конкретного типа гейта и отрисовка его полей. Заменяет малозаметный встроенный пикер.
    /// </summary>
    [CustomPropertyDrawer(typeof(StepGate), true)]
    internal sealed class StepGatePropertyDrawer : PropertyDrawer
    {
        private static List<Type> gateTypes;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            bool hasValue = !string.IsNullOrEmpty(property.managedReferenceFullTypename);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(line.x, line.y, EditorGUIUtility.labelWidth, line.height);
            var dropdownRect = new Rect(line.x + EditorGUIUtility.labelWidth, line.y,
                line.width - EditorGUIUtility.labelWidth, line.height);

            if (hasValue)
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
            else
                EditorGUI.LabelField(labelRect, label);

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(CurrentTypeName(property)), FocusType.Keyboard))
                ShowTypeMenu(property);

            if (hasValue && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;

                foreach (var child in VisibleChildren(property))
                {
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!string.IsNullOrEmpty(property.managedReferenceFullTypename) && property.isExpanded)
                foreach (var child in VisibleChildren(property))
                    height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        private static IEnumerable<SerializedProperty> VisibleChildren(SerializedProperty property)
        {
            var child = property.Copy();
            var end = child.GetEndProperty();

            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                yield return child.Copy();
            }
        }

        private static string CurrentTypeName(SerializedProperty property)
        {
            string full = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return "Нет";

            // Формат: "<Assembly> <Namespace>.<Type>"
            int space = full.IndexOf(' ');
            string typeName = space >= 0 ? full[(space + 1)..] : full;
            int dot = typeName.LastIndexOf('.');
            return dot >= 0 ? typeName[(dot + 1)..] : typeName;
        }

        private static void ShowTypeMenu(SerializedProperty property)
        {
            gateTypes ??= CollectGateTypes();

            var serializedObject = property.serializedObject;
            string path = property.propertyPath;
            bool isNull = string.IsNullOrEmpty(property.managedReferenceFullTypename);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Нет"), isNull, () => Assign(serializedObject, path, null));

            foreach (var type in gateTypes)
            {
                var captured = type;
                menu.AddItem(new GUIContent(type.Name), false, () => Assign(serializedObject, path, captured));
            }

            menu.ShowAsContext();
        }

        private static void Assign(SerializedObject serializedObject, string path, Type type)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(path);
            if (property == null) return;

            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
        }

        private static List<Type> CollectGateTypes()
        {
            var list = new List<Type>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<StepGate>())
                if (!type.IsAbstract && !type.IsGenericType && type.GetConstructor(Type.EmptyTypes) != null)
                    list.Add(type);

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return list;
        }
    }
}
