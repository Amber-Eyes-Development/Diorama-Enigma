using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Инспектор ввода: плашка-предупреждение об отсутствии коллайдера (без него взаимодействие не ловится)
    /// </summary>
    [CustomEditor(typeof(InteractableInput), editorForChildClasses: true)]
    internal sealed class InteractableInputEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var input = (InteractableInput)target;

            if (input.GetComponentInChildren<Collider>() == null)
                EditorGUILayout.HelpBox(
                    "Нет коллайдера на объекте или его детях — взаимодействие (клик/драг) работать не будет.",
                    MessageType.Warning);

            DrawDefaultInspector();
        }
    }
}
