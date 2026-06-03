using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Окно обзора интерактивных объектов и зон в сцене
    /// </summary>
    public sealed class RiddleSceneObjectsWindow : EditorWindow
    {
        private readonly List<InteractableObject> interactables = new();
        private readonly List<DropZoneObject> dropZones = new();

        private Vector2 scroll;
        private double lastRefreshTime;

        private GUIStyle styleSectionHeader;
        private GUIStyle styleSmall;

        [MenuItem("Diorama Enigma/Riddle Scene Objects", priority = 101)]
        public static void Open()
        {
            var window = GetWindow<RiddleSceneObjectsWindow>("Riddle Scene Objects");
            window.minSize = new Vector2(360f, 240f);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Refresh();
        }

        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastRefreshTime > 2.0)
                Refresh();

            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar,
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT));
            EditorGUILayout.LabelField($"Объектов: {interactables.Count}   Зон: {dropZones.Count}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↺ Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (interactables.Count == 0 && dropZones.Count == 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorGUILayout.LabelField("  InteractableObject и DropZoneObject в сцене не найдены", styleSmall);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (interactables.Count > 0)
            {
                ColorLabel("  ИНТЕРАКТИВНЫЕ ОБЪЕКТЫ", EditorToolsConstraints.COLOR_LIGHT_GREEN, styleSectionHeader);
                foreach (var obj in interactables)
                {
                    if (obj == null) continue;
                    DrawRow(obj.gameObject, ObjectInfo(obj), EditorToolsConstraints.COLOR_LIGHT_GREEN);
                }
            }

            if (dropZones.Count > 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                ColorLabel("  ЗОНЫ ПРИЗЕМЛЕНИЯ", EditorToolsConstraints.COLOR_YELLOW, styleSectionHeader);
                foreach (var zone in dropZones)
                {
                    if (zone == null) continue;
                    DrawRow(zone.gameObject, $"◈ {zone.gameObject.name}   id: {zone.ZoneId}", EditorToolsConstraints.COLOR_YELLOW);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(GameObject go, string label, Color color)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            ColorLabel(label, color, styleSmall);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping", GUILayout.Width(44), GUILayout.Height(16)))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string ObjectInfo(InteractableObject obj)
        {
            string playInfo = Application.isPlaying
                ? $"   state:{obj.State.Current}  {(obj.IsLocked ? "🔒" : "🔓")}"
                : string.Empty;

            return $"● {obj.gameObject.name}   id: {obj.Id}{playInfo}";
        }

        private void Refresh()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;

            interactables.Clear();
            dropZones.Clear();

#pragma warning disable CS0618
            interactables.AddRange(FindObjectsOfType<InteractableObject>());
            dropZones.AddRange(FindObjectsOfType<DropZoneObject>());
#pragma warning restore CS0618

            Repaint();
        }

        private static void ColorLabel(string text, Color color, GUIStyle style)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(text, style);
            GUI.contentColor = prev;
        }

        private void EnsureStyles()
        {
            styleSectionHeader ??= new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1 };

            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        }
    }
}
