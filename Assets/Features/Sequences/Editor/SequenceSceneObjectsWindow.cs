using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Окно обзора интерактивных компонентов и зон в сцене
    /// </summary>
    public sealed class SequenceSceneObjectsWindow : EditorWindow
    {
        private class ClickEntry
        {
            public ClickInteractable Component;
            public AbstractSequenceStep StateRef;
        }

        private class DragEntry
        {
            public DraggableInteractable Component;
            public AbstractSequenceStep StateRef;
            public string TargetZoneName;
        }

        private readonly List<ClickEntry> clicks = new();
        private readonly List<DragEntry> drags = new();
        private readonly List<DropZoneObject> dropZones = new();

        private Vector2 scroll;
        private double lastRefreshTime;

        private GUIStyle styleSectionHeader;
        private GUIStyle styleSmall;

        [MenuItem("Diorama Enigma/Step Objects in Scene", priority = 101)]
        public static void Open()
        {
            var window = GetWindow<SequenceSceneObjectsWindow>("Step Objects in Scene");
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

            int total = clicks.Count + drags.Count;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar,
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT));
            EditorGUILayout.LabelField($"Клик: {clicks.Count}   Перетаск: {drags.Count}   Зоны: {dropZones.Count}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↺ Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (total == 0 && dropZones.Count == 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorGUILayout.LabelField("  ClickInteractable, DraggableInteractable и DropZoneObject в сцене не найдены", styleSmall);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (clicks.Count > 0)
            {
                ColorLabel("  КЛИК", EditorToolsConstraints.COLOR_CYAN, styleSectionHeader);
                foreach (var entry in clicks)
                {
                    if (entry?.Component == null) continue;
                    DrawRow(entry.Component.gameObject, ClickLabel(entry), EditorToolsConstraints.COLOR_CYAN);
                }
            }

            if (drags.Count > 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                ColorLabel("  ПЕРЕТАСКИВАНИЕ", EditorToolsConstraints.COLOR_YELLOW, styleSectionHeader);
                foreach (var entry in drags)
                {
                    if (entry?.Component == null) continue;
                    DrawRow(entry.Component.gameObject, DragLabel(entry), EditorToolsConstraints.COLOR_YELLOW);
                }
            }

            if (dropZones.Count > 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                ColorLabel("  ЗОНЫ ПРИЗЕМЛЕНИЯ", EditorToolsConstraints.COLOR_LIGHT_GREEN, styleSectionHeader);
                foreach (var zone in dropZones)
                {
                    if (zone == null) continue;
                    DrawRow(zone.gameObject, $"◈ {zone.gameObject.name}", EditorToolsConstraints.COLOR_LIGHT_GREEN);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private const string SEP = "  |  ";

        private static string ClickLabel(ClickEntry entry)
        {
            string stateName = entry.StateRef != null ? entry.StateRef.name : "(не назначено)";
            return $"◉ {entry.Component.gameObject.name}{SEP}state: {stateName}{StepStateInfo(entry.StateRef)}{PlayInfo(entry.StateRef)}";
        }

        private static string DragLabel(DragEntry entry)
        {
            string stateName = entry.StateRef != null ? entry.StateRef.name : "(не назначено)";
            string zoneName = string.IsNullOrEmpty(entry.TargetZoneName) ? "любая" : entry.TargetZoneName;
            return $"⬡ {entry.Component.gameObject.name}{SEP}state: {stateName}{SEP}zone: {zoneName}{StepStateInfo(entry.StateRef)}{PlayInfo(entry.StateRef)}";
        }

        /// <summary> Целевое и текущее значение булева шага (true/false) </summary>
        private static string StepStateInfo(AbstractSequenceStep step)
        {
            if (step is not SequenceStep valueStep) return string.Empty;
            return $"{SEP}цель: {Bool(valueStep.CompletionState)}{SEP}тек: {Bool(valueStep.Value)}";
        }

        /// <summary> Индикатор завершённости в Play Mode </summary>
        private static string PlayInfo(AbstractSequenceStep step)
        {
            return Application.isPlaying && step != null
                ? $"{SEP}{(step.IsCompleted ? "✓" : "…")}"
                : string.Empty;
        }

        private static string Bool(bool value) => value ? "true" : "false";

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

        private void Refresh()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;

            clicks.Clear();
            drags.Clear();
            dropZones.Clear();

            foreach (var click in FindObjectsByType<ClickInteractable>(FindObjectsSortMode.None))
            {
                var stateRef = click.GetComponent<StepReference>()?.Step;
                clicks.Add(new ClickEntry { Component = click, StateRef = stateRef });
            }

            foreach (var drag in FindObjectsByType<DraggableInteractable>(FindObjectsSortMode.None))
            {
                var stateRef = drag.GetComponent<StepReference>()?.Step;
                var zone = drag.Editor_TargetZone;
                drags.Add(new DragEntry
                {
                    Component = drag,
                    StateRef = stateRef,
                    TargetZoneName = zone != null ? zone.gameObject.name : string.Empty,
                });
            }

            dropZones.AddRange(FindObjectsByType<DropZoneObject>(FindObjectsSortMode.None));

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
