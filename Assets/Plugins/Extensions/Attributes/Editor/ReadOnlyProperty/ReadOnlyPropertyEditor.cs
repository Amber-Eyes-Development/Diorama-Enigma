using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Extensions.Attributes.Editor
{
    /// <summary>
    /// Инспектор для вывода свойств с <see cref="ReadOnlyPropertyAttribute"/>
    /// </summary>
    public abstract class ReadOnlyPropertyEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<Type, PropertyData[]> PropertiesCache = new();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawReadOnlyProperties();
        }

        private void DrawReadOnlyProperties()
        {
            PropertyData[] properties = GetProperties(target.GetType());

            if (properties.Length == 0)
                return;

            EditorGUILayout.Space();

            foreach (PropertyData property in properties)
                DrawProperty(property);
        }

        private void DrawProperty(PropertyData property)
        {
            object value;

            try
            {
                value = property.Info.GetValue(target);
            }
            catch (Exception exception)
            {
                Exception sourceException = exception is TargetInvocationException invocationException
                    ? invocationException.InnerException ?? exception
                    : exception;

                EditorGUILayout.HelpBox($"{property.Label.text}: {sourceException.Message}", MessageType.Error);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
                DrawValue(property.Label, property.Info.PropertyType, value);
        }

        private static void DrawValue(GUIContent label, Type propertyType, object value)
        {
            Type valueType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                EditorGUILayout.ObjectField(label, value as UnityEngine.Object, valueType, true);
                return;
            }

            if (value == null)
            {
                EditorGUILayout.TextField(label, "null");
                return;
            }

            if (valueType.IsEnum)
            {
                EditorGUILayout.EnumPopup(label, (Enum)value);
                return;
            }

            switch (value)
            {
                case bool boolValue:
                    EditorGUILayout.Toggle(label, boolValue);
                    break;
                case int intValue:
                    EditorGUILayout.IntField(label, intValue);
                    break;
                case long longValue:
                    EditorGUILayout.LongField(label, longValue);
                    break;
                case float floatValue:
                    EditorGUILayout.FloatField(label, floatValue);
                    break;
                case double doubleValue:
                    EditorGUILayout.DoubleField(label, doubleValue);
                    break;
                case string stringValue:
                    EditorGUILayout.TextField(label, stringValue);
                    break;
                case Vector2 vector2Value:
                    EditorGUILayout.Vector2Field(label, vector2Value);
                    break;
                case Vector3 vector3Value:
                    EditorGUILayout.Vector3Field(label, vector3Value);
                    break;
                case Vector4 vector4Value:
                    EditorGUILayout.Vector4Field(label, vector4Value);
                    break;
                case Vector2Int vector2IntValue:
                    EditorGUILayout.Vector2IntField(label, vector2IntValue);
                    break;
                case Vector3Int vector3IntValue:
                    EditorGUILayout.Vector3IntField(label, vector3IntValue);
                    break;
                case Color colorValue:
                    EditorGUILayout.ColorField(label, colorValue);
                    break;
                case Rect rectValue:
                    EditorGUILayout.RectField(label, rectValue);
                    break;
                case RectInt rectIntValue:
                    EditorGUILayout.RectIntField(label, rectIntValue);
                    break;
                case Bounds boundsValue:
                    EditorGUILayout.BoundsField(label, boundsValue);
                    break;
                case BoundsInt boundsIntValue:
                    EditorGUILayout.BoundsIntField(label, boundsIntValue);
                    break;
                case AnimationCurve curveValue:
                    EditorGUILayout.CurveField(label, curveValue);
                    break;
                case Gradient gradientValue:
                    EditorGUILayout.GradientField(label, gradientValue);
                    break;
                default:
                    EditorGUILayout.LabelField(label, new GUIContent(value.ToString()));
                    break;
            }
        }

        private static PropertyData[] GetProperties(Type targetType)
        {
            if (PropertiesCache.TryGetValue(targetType, out PropertyData[] properties))
                return properties;

            List<PropertyData> result = new();
            Stack<Type> hierarchy = new();

            for (Type type = targetType; type != null && type != typeof(UnityEngine.Object); type = type.BaseType)
                hierarchy.Push(type);

            while (hierarchy.Count > 0)
            {
                Type type = hierarchy.Pop();
                PropertyInfo[] typeProperties = type.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (PropertyInfo property in typeProperties)
                {
                    ReadOnlyPropertyAttribute propertyAttribute = property.GetCustomAttribute<ReadOnlyPropertyAttribute>();

                    if (propertyAttribute == null || property.GetGetMethod(true) == null || property.GetIndexParameters().Length > 0)
                        continue;

                    string label = string.IsNullOrWhiteSpace(propertyAttribute.Label)
                        ? ObjectNames.NicifyVariableName(property.Name)
                        : propertyAttribute.Label;

                    result.Add(new PropertyData(property, new GUIContent(label)));
                }
            }

            properties = result.ToArray();
            PropertiesCache[targetType] = properties;
            return properties;
        }

        private readonly struct PropertyData
        {
            public readonly PropertyInfo Info;
            public readonly GUIContent Label;

            public PropertyData(PropertyInfo info, GUIContent label)
            {
                Info = info;
                Label = label;
            }
        }
    }
}
