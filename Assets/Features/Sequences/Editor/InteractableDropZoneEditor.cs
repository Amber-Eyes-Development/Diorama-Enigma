using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Инспектор зоны дропа: плашка-предупреждение об отсутствии коллайдера или о том, что он не триггер
    /// </summary>
    [CustomEditor(typeof(InteractableDropZone))]
    internal sealed class InteractableDropZoneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var zone = (InteractableDropZone)target;
            var zoneCollider = zone.GetComponentInChildren<Collider>();

            if (zoneCollider == null)
                EditorGUILayout.HelpBox(
                    "Нет коллайдера — зона дропа не сможет принимать объекты.",
                    MessageType.Warning);
            else if (!zoneCollider.isTrigger)
                EditorGUILayout.HelpBox(
                    "Коллайдер зоны дропа должен быть триггером (Is Trigger).",
                    MessageType.Warning);

            DrawDefaultInspector();
        }
    }
}
