using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Общий инспектор реакций (цвет фона скрипта для индикации)
    /// </summary>
    [CustomEditor(typeof(InteractableReactionBehaviour), editorForChildClasses: true)]
    internal sealed class InteractableReactionBehaviourEditor : UnityEditor.Editor
    {
        private static readonly Color Tint = new(0.77f, 0.5f, 0.82f, 0.075f);

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                VisualElement panel = root.GetFirstAncestorOfType<InspectorElement>()?.parent;
                if (panel != null)
                    panel.style.backgroundColor = Tint;
            });

            return root;
        }
    }
}
