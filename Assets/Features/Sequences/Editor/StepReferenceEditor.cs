using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Инспектор <see cref="StepReference"/>: показывает поле ссылки на шаг и встроенный
    /// инспектор самого ассета-шага. Правки идут напрямую в ассет и сохраняются автоматически.
    /// </summary>
    [CustomEditor(typeof(StepReference))]
    internal sealed class StepReferenceEditor : UnityEditor.Editor
    {
        private const string FoldoutKey = "DioramaEnigma.StepReference.embeddedFoldout";

        private SerializedProperty stepProp;
        private UnityEditor.Editor embeddedEditor;

        private void OnEnable() => stepProp = serializedObject.FindProperty("step");

        private void OnDisable()
        {
            if (embeddedEditor != null) DestroyImmediate(embeddedEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(stepProp);
            serializedObject.ApplyModifiedProperties();

            var step = stepProp.objectReferenceValue;
            if (step == null)
            {
                EditorGUILayout.HelpBox("Шаг не назначен.", MessageType.Info);
                ReleaseEmbedded();
                return;
            }

            // Пересоздаём встроенный редактор, если ссылка сменилась
            if (embeddedEditor == null || embeddedEditor.target != step)
            {
                ReleaseEmbedded();
                embeddedEditor = CreateEditor(step);
            }

            EditorGUILayout.Space(8);
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUILayout.Space(4);

            bool expanded = EditorPrefs.GetBool(FoldoutKey, true);
            bool newExpanded = EditorGUILayout.Foldout(expanded, "Параметры шага", true, EditorStyles.foldoutHeader);
            if (newExpanded != expanded) EditorPrefs.SetBool(FoldoutKey, newExpanded);

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    embeddedEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }

        private void ReleaseEmbedded()
        {
            if (embeddedEditor == null) return;
            DestroyImmediate(embeddedEditor);
            embeddedEditor = null;
        }
    }
}
