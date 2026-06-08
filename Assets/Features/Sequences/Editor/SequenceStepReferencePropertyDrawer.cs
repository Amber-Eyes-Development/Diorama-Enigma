using Extensions.Identification;
using Extensions.Log;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Дровер для <see cref="SequenceStepReferenceAttribute"/>: ObjectField, принимающий
    /// только ассеты-шаги (<see cref="ISequenceStep"/>); прочие объекты отклоняются.
    /// </summary>
    [CustomPropertyDrawer(typeof(SequenceStepReferenceAttribute))]
    internal sealed class SequenceStepReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            var assigned = EditorGUI.ObjectField(position, label, property.objectReferenceValue,
                typeof(IdentifiableObject), false);

            if (EditorGUI.EndChangeCheck())
            {
                if (assigned == null || assigned is ISequenceStep)
                    property.objectReferenceValue = assigned;
                else
                    ServiceDebug.LogWarning(assigned,
                        $"«{assigned.name}» не реализует {nameof(ISequenceStep)} — ссылку нельзя назначить как шаг.");
            }

            EditorGUI.EndProperty();
        }
    }
}
