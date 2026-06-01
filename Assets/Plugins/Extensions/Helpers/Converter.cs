#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Extensions.Helpers
{
    /// <summary>
    /// Конвертер компонентов
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// Конвертация компонентов
        /// </summary>
        /// <returns></returns>
        public static TTarget ConvertComponent<TSource, TTarget>(TSource source)
            where TSource : Component
            where TTarget : Component
        {
            GameObject go = source.gameObject;

            Undo.RegisterFullObjectHierarchyUndo(go, $"Convert {typeof(TSource).Name} To {typeof(TTarget).Name}");

            TTarget newComponent = Undo.AddComponent<TTarget>(go);

            CopySerializedFields(source, newComponent);

            Undo.DestroyObjectImmediate(source);

            EditorUtility.SetDirty(go);

            return newComponent;
        }

        private static void CopySerializedFields(Object source, Object target)
        {
            SerializedObject sourceSO = new SerializedObject(source);
            SerializedObject targetSO = new SerializedObject(target);

            SerializedProperty iterator = sourceSO.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                    continue;

                SerializedProperty targetProperty = targetSO.FindProperty(iterator.propertyPath);

                if (targetProperty == null)
                    continue;

                CopyPropertyValue(iterator, targetProperty);
            }

            targetSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopyPropertyValue(SerializedProperty source, SerializedProperty target)
        {
            if (source.propertyType != target.propertyType)
                return;

            switch (source.propertyType)
            {
                case SerializedPropertyType.Integer:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    target.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    target.floatValue = source.floatValue;
                    break;
                case SerializedPropertyType.String:
                    target.stringValue = source.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    target.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    target.objectReferenceValue = source.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    target.enumValueIndex = source.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    target.vector2Value = source.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    target.vector3Value = source.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    target.vector4Value = source.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    target.rectValue = source.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Character:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    target.animationCurveValue = source.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    target.boundsValue = source.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    target.gradientValue = source.gradientValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    target.quaternionValue = source.quaternionValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    target.exposedReferenceValue = source.exposedReferenceValue;
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    target.vector2IntValue = source.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    target.vector3IntValue = source.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    target.rectIntValue = source.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    target.boundsIntValue = source.boundsIntValue;
                    break;
                case SerializedPropertyType.ManagedReference:
                    target.managedReferenceValue = source.managedReferenceValue;
                    break;
                default:
                    target.serializedObject.CopyFromSerializedProperty(source);
                    break;
            }
        }
    }
}
#endif