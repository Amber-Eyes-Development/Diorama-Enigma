using System;
using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Окно обзора интерактивных объектов сцены: объекты относятся к раннеру той последовательности,
    /// которой они принадлежат (раннер с раскрытием инспектора, затем его объекты)
    /// </summary>
    public sealed class SequenceSceneObjectsWindow : EditorWindow
    {
        private const float SQUARE = EditorToolsConstraints.BASE_ELEMENT_HEIGHT;
        private const string SEP = "  |  ";

        private sealed class ClickEntry
        {
            public ClickInteractable Component;
            public AbstractSequenceStep StateRef;
        }

        private sealed class DragEntry
        {
            public DraggableInteractable Component;
            public AbstractSequenceStep StateRef;
            public string TargetZoneName;
        }

        private sealed class ObjectEntry
        {
            public GameObject Go;
            public Color Color;
            public Func<string> Label;
        }

        private readonly List<SequenceRunner> runners = new();
        private readonly Dictionary<Sequence, List<ObjectEntry>> objectsBySequence = new();
        private readonly List<ObjectEntry> orphanObjects = new();

        private readonly HashSet<int> expandedRunners = new();
        private readonly Dictionary<int, UnityEditor.Editor> runnerEditors = new();

        private Vector2 scroll;
        private double lastRefreshTime;

        private GUIStyle styleSmall;
        private GUIStyle styleRunnerButton;
        private GUIStyle styleOrphanHeader;

        [MenuItem("Diorama Enigma/Step Objects in Scene", priority = 101)]
        public static void Open()
        {
            var window = GetWindow<SequenceSceneObjectsWindow>("Step Objects in Scene");
            window.minSize = new Vector2(380f, 260f);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            ReleaseRunnerEditors();
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastRefreshTime > 2.0)
                Refresh();

            if (Application.isPlaying)
                Repaint();
        }

        #region GUI

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (runners.Count == 0 && orphanObjects.Count == 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorGUILayout.LabelField("  Раннеры и интерактивные объекты в сцене не найдены", styleSmall);
                EditorGUILayout.EndScrollView();
                return;
            }

            var rendered = new HashSet<Sequence>();

            foreach (var runner in runners)
            {
                DrawRunnerRow(runner);

                var sequence = runner.Editor_Sequence;
                if (sequence == null || !rendered.Add(sequence)) continue;

                if (objectsBySequence.TryGetValue(sequence, out var entries))
                    foreach (var entry in entries)
                        DrawObjectRow(entry);
            }

            DrawOrphans(rendered);

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            int objects = orphanObjects.Count;
            foreach (var list in objectsBySequence.Values) objects += list.Count;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(SQUARE));
            EditorGUILayout.LabelField($"Раннеров: {runners.Count}   Объектов: {objects}");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↺ Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRunnerRow(SequenceRunner runner)
        {
            var separator = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(separator, new Color(0.45f, 0.65f, 0.95f, 1f));
            EditorGUILayout.Space(2);

            int id = runner.GetInstanceID();
            bool expanded = expandedRunners.Contains(id);
            string sequenceName = runner.Editor_Sequence != null ? runner.Editor_Sequence.name : "(нет последовательности)";

            EditorGUILayout.BeginHorizontal();

            string indicator = expanded ? "▾" : "▸";
            GUI.backgroundColor = EditorToolsConstraints.COLOR_ACCENT;
            if (GUILayout.Button($"  {indicator}  ▶ {runner.gameObject.name}   ({sequenceName})", styleRunnerButton, GUILayout.Height(SQUARE)))
            {
                if (!expandedRunners.Remove(id)) expandedRunners.Add(id);
            }
            GUI.backgroundColor = Color.white;

            DrawPingButton(runner.gameObject);

            EditorGUILayout.EndHorizontal();

            if (expanded)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var editor = GetRunnerEditor(runner);
                    if (editor != null) editor.OnInspectorGUI();
                }
        }

        private void DrawOrphans(HashSet<Sequence> rendered)
        {
            // Объекты последовательностей без раннера в сцене + объекты без последовательности + зоны
            bool any = orphanObjects.Count > 0;
            foreach (var pair in objectsBySequence)
                if (!rendered.Contains(pair.Key)) { any = true; break; }

            if (!any) return;

            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            var separator = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(separator, new Color(0.5f, 0.5f, 0.5f, 1f));
            EditorGUILayout.LabelField("Без раннера", styleOrphanHeader);

            foreach (var pair in objectsBySequence)
                if (!rendered.Contains(pair.Key))
                    foreach (var entry in pair.Value)
                        DrawObjectRow(entry);

            foreach (var entry in orphanObjects)
                DrawObjectRow(entry);
        }

        private void DrawObjectRow(ObjectEntry entry)
        {
            if (entry.Go == null) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            ColorLabel(entry.Label(), entry.Color, styleSmall);
            GUILayout.FlexibleSpace();
            DrawPingButton(entry.Go);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPingButton(GameObject go)
        {
            GUI.backgroundColor = EditorToolsConstraints.COLOR_CYAN;
            if (GUILayout.Button(EditorToolsConstraints.SYMBOL_PING, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Labels

        private static string ClickLabel(ClickEntry entry)
        {
            string stateName = entry.StateRef != null ? entry.StateRef.name : "(не назначено)";
            return $"◉ {entry.Component.gameObject.name}{SEP}state: {stateName}{StepStateInfo(entry.StateRef)}{PlayInfo(entry.StateRef)}";
        }

        private static string DragLabel(DragEntry entry)
        {
            string stateName = entry.StateRef != null ? entry.StateRef.name : "(не назначено)";
            string zoneName = string.IsNullOrEmpty(entry.TargetZoneName) ? "(нет зоны)" : entry.TargetZoneName;
            return $"⬡ {entry.Component.gameObject.name}{SEP}state: {stateName}{SEP}zone: {zoneName}{StepStateInfo(entry.StateRef)}{PlayInfo(entry.StateRef)}";
        }

        private static string StepStateInfo(AbstractSequenceStep step)
        {
            if (step is not SequenceStep valueStep) return string.Empty;
            return $"{SEP}доступ: {Bool(step.IsUnlocked)}{SEP}цель: {Bool(valueStep.CompletionState)}{SEP}тек: {Bool(valueStep.Value)}";
        }

        private static string PlayInfo(AbstractSequenceStep step) =>
            Application.isPlaying && step != null
                ? $"{SEP}{(step.IsCompleted ? "✓" : "…")}"
                : string.Empty;

        private static string Bool(bool value) => value ? "true" : "false";

        #endregion

        #region Build

        private void Refresh()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;

            runners.Clear();
            objectsBySequence.Clear();
            orphanObjects.Clear();

            var stepToSequence = BuildStepToSequence();

            runners.AddRange(FindObjectsByType<SequenceRunner>(FindObjectsSortMode.None));
            runners.Sort((a, b) => string.Compare(a.gameObject.name, b.gameObject.name, StringComparison.OrdinalIgnoreCase));

            foreach (var click in FindObjectsByType<ClickInteractable>(FindObjectsSortMode.None))
            {
                var entry = new ClickEntry { Component = click, StateRef = click.GetComponent<StepReference>()?.Step };
                Add(SequenceOf(entry.StateRef, stepToSequence), new ObjectEntry
                {
                    Go = click.gameObject,
                    Color = EditorToolsConstraints.COLOR_CYAN,
                    Label = () => ClickLabel(entry),
                });
            }

            var stepToZoneName = new Dictionary<AbstractSequenceStep, string>();
            foreach (var zone in FindObjectsByType<InteractableDropZone>(FindObjectsSortMode.None))
            {
                var captured = zone;
                if (zone.Step != null) stepToZoneName.TryAdd(zone.Step, zone.gameObject.name);

                orphanObjects.Add(new ObjectEntry
                {
                    Go = captured.gameObject,
                    Color = EditorToolsConstraints.COLOR_LIGHT_GREEN,
                    Label = () => $"◈ {captured.gameObject.name}",
                });
            }

            foreach (var drag in FindObjectsByType<DraggableInteractable>(FindObjectsSortMode.None))
            {
                var step = drag.GetComponent<StepReference>()?.Step;
                var entry = new DragEntry
                {
                    Component = drag,
                    StateRef = step,
                    TargetZoneName = step != null && stepToZoneName.TryGetValue(step, out var zoneName) ? zoneName : string.Empty,
                };
                Add(SequenceOf(entry.StateRef, stepToSequence), new ObjectEntry
                {
                    Go = drag.gameObject,
                    Color = EditorToolsConstraints.COLOR_YELLOW,
                    Label = () => DragLabel(entry),
                });
            }

            SortByName(orphanObjects);
            foreach (var list in objectsBySequence.Values) SortByName(list);

            Repaint();
        }

        private void Add(Sequence sequence, ObjectEntry entry)
        {
            if (sequence == null)
            {
                orphanObjects.Add(entry);
                return;
            }

            if (!objectsBySequence.TryGetValue(sequence, out var list))
                objectsBySequence[sequence] = list = new List<ObjectEntry>();

            list.Add(entry);
        }

        private static void SortByName(List<ObjectEntry> list) =>
            list.Sort((a, b) => string.Compare(
                a.Go != null ? a.Go.name : string.Empty,
                b.Go != null ? b.Go.name : string.Empty,
                StringComparison.OrdinalIgnoreCase));

        private static Sequence SequenceOf(AbstractSequenceStep step, Dictionary<int, Sequence> lookup)
        {
            if (step == null) return null;
            return lookup.TryGetValue(step.GetInstanceID(), out var seq) ? seq : null;
        }

        private static Dictionary<int, Sequence> BuildStepToSequence()
        {
            var map = new Dictionary<int, Sequence>();

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Sequence)}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var seq = AssetDatabase.LoadAssetAtPath<Sequence>(path);
                if (seq == null) continue;

                foreach (var entry in seq.Steps)
                {
                    if (entry?.Step == null) continue;
                    map.TryAdd(entry.Step.GetInstanceID(), seq);
                }
            }

            return map;
        }

        #endregion

        #region Helpers

        private UnityEditor.Editor GetRunnerEditor(SequenceRunner runner)
        {
            int id = runner.GetInstanceID();

            if (runnerEditors.TryGetValue(id, out var editor) && editor != null && editor.target == runner)
                return editor;

            if (editor != null) DestroyImmediate(editor);

            editor = UnityEditor.Editor.CreateEditor(runner);
            runnerEditors[id] = editor;
            return editor;
        }

        private void ReleaseRunnerEditors()
        {
            foreach (var editor in runnerEditors.Values)
                if (editor != null) DestroyImmediate(editor);

            runnerEditors.Clear();
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
            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

            styleRunnerButton ??= new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                padding = new RectOffset(8, 6, 2, 2),
            };

            styleOrphanHeader ??= new GUIStyle(EditorStyles.boldLabel)
                { fontSize = EditorToolsConstraints.BASE_FONT_SIZE };
        }

        #endregion
    }
}
