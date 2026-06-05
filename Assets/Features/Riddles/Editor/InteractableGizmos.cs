using Extensions.ScriptableValues;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Гизмо в сцене для ClickInteractable и DraggableInteractable:
    /// label с именем шага + значением в Play Mode; пунктирная линия к целевой зоне.
    /// </summary>
    internal static class InteractableGizmos
    {
        private const float LabelOffsetY = 0.35f;

        private static GUIStyle _clickStyle;
        private static GUIStyle _dragStyle;

        private static GUIStyle ClickStyle => _clickStyle ??= BuildStyle(new Color(0.4f, 1f, 1f));
        private static GUIStyle DragStyle  => _dragStyle  ??= BuildStyle(new Color(1f, 0.9f, 0.35f));

        // ─── Click ────────────────────────────────────────────────────────────

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawClickGizmo(ClickInteractable click, GizmoType gizmoType)
        {
            bool inSelection = (gizmoType & (GizmoType.InSelectionHierarchy | GizmoType.Selected)) != 0;
            if (!Application.isPlaying && !inSelection) return;

            var stateRef = click.Editor_StateRef;
            if (stateRef == null) return;

            string text = BuildLabel("◉", stateRef, stateRef.Value.ToString());
            Handles.Label(click.transform.position + Vector3.up * LabelOffsetY, text, ClickStyle);
        }

        // ─── Drag ─────────────────────────────────────────────────────────────

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawDragGizmo(DraggableInteractable drag, GizmoType gizmoType)
        {
            bool inSelection = (gizmoType & (GizmoType.InSelectionHierarchy | GizmoType.Selected)) != 0;
            if (!Application.isPlaying && !inSelection) return;

            var stateRef = drag.Editor_StateRef;
            if (stateRef == null) return;

            string text = BuildLabel("⬡", stateRef, stateRef.Value.ToString());
            Handles.Label(drag.transform.position + Vector3.up * LabelOffsetY, text, DragStyle);

            // Dotted line to target zone (edit mode only, when selected)
            var zone = drag.Editor_TargetZone;
            if (zone != null && inSelection)
            {
                var prevColor = Handles.color;
                Handles.color = new Color(1f, 0.9f, 0.35f, 0.55f);
                Handles.DrawDottedLine(drag.transform.position, zone.transform.position, 4f);
                Handles.color = prevColor;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static string BuildLabel(string icon, BaseScriptableValue stateRef, string valueStr)
        {
            string name = stateRef is IPuzzleStep ps && !string.IsNullOrEmpty(ps.StepLabel)
                ? ps.StepLabel
                : stateRef.name;

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
    }
}
