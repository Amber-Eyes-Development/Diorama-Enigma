using UnityEditor;
using UnityEngine;

namespace Extensions.Identification.Editor
{
    /// <summary>
    /// Общий инспектор реестров идентифицируемых объектов
    /// </summary>
    /// <remarks>
    /// Таргетит негенерик-базис <see cref="IdentifiableRegistry"/> с наследованием — покрывает все реестры
    /// </remarks>
    [CustomEditor(typeof(IdentifiableRegistry), true)]
    public class IdentifiableRegistryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            IdentifiableRegistry registry = (IdentifiableRegistry)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Collect All"))
                {
                    Undo.RecordObject(registry, "Collect All Registry Entries");
                    registry.CollectAllInEditor();
                }

                if (GUILayout.Button("Validate"))
                    registry.ValidateInEditor();
            }

            GUILayout.Space(8f);

            DrawDefaultInspector();
        }
    }
}
