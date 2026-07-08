using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Inventory.Editor
{
    /// <summary>
    /// Инспектор предмета
    /// </summary>
    [CustomEditor(typeof(ResourceValue))]
    internal sealed class ResourceValueEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawPreview();
        }

        /// <summary> Текущее количество ресурса </summary>
        private void DrawPreview()
        {
            if (target is not ResourceValue resource) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Количество (превью)", EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("По умолчанию", resource.DefaultValue);

                if (Application.isPlaying)
                    EditorGUILayout.IntField("Текущее", resource.Value);
            }
        }
    }
}
