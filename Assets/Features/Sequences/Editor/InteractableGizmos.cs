using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Гизмо в сцене для <see cref="InteractableInput"/>
    /// </summary>
    internal static class InteractableGizmos
    {
        private const float LabelOffsetY = 0.35f;

        private static GUIStyle _clickStyle;
        private static GUIStyle _dragStyle;

        private static GUIStyle ClickStyle => _clickStyle ??= BuildStyle(new Color(0.4f, 1f, 1f));
        private static GUIStyle DragStyle  => _dragStyle  ??= BuildStyle(new Color(1f, 0.9f, 0.35f));

        #region Click

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawClickGizmo(ClickInteractable click, GizmoType gizmoType)
        {
            var stateRef = click.GetComponent<StepReference>()?.Step;
            string text = stateRef != null
                ? BuildLabel("◉", stateRef, ValueString(stateRef))
                : "◉ (шаг не задан)";

            Handles.Label(click.transform.position + Vector3.up * LabelOffsetY, text, ClickStyle);
        }

        #endregion

        #region Drag

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawDragGizmo(DraggableInteractable drag, GizmoType gizmoType)
        {
            bool inSelection = (gizmoType & (GizmoType.InSelectionHierarchy | GizmoType.Selected)) != 0;

            var stateRef = drag.GetComponent<StepReference>()?.Step;
            string text = stateRef != null
                ? BuildLabel("⬡", stateRef, ValueString(stateRef))
                : "⬡ (шаг не задан)";

            Handles.Label(drag.transform.position + Vector3.up * LabelOffsetY, text, DragStyle);

            // Пунктирная линия к целевой зоне / точке фиксации (только при выделении)
            var zone = drag.Editor_TargetZone;
            if (zone != null && inSelection)
            {
                Vector3 target = zone.Anchor != null ? zone.Anchor.position : zone.transform.position;

                var prevColor = Handles.color;
                Handles.color = new Color(1f, 0.9f, 0.35f, 0.55f);
                Handles.DrawDottedLine(drag.transform.position, target, 4f);
                if (zone.Anchor != null)
                    Handles.SphereHandleCap(0, target, Quaternion.identity, 0.08f, EventType.Repaint);
                Handles.color = prevColor;
            }
        }

        #endregion

        #region Helpers

        private static string ValueString(AbstractSequenceStep step) =>
            step != null ? (step.IsCompleted ? "✓" : "…") : string.Empty;

        private static string BuildLabel(string icon, AbstractSequenceStep stateRef, string valueStr)
        {
            string name = stateRef.name;

            return Application.isPlaying
                ? $"{icon} {name}  [{valueStr}]"
                : $"{icon} {name}";
        }

        private static GUIStyle BuildStyle(Color textColor)
        {
            return new GUIStyle
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = textColor },
            };
        }

        #endregion
    }
}
