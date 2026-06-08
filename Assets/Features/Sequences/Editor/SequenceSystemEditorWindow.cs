using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Навигатор последовательностей: все <see cref="Sequence"/> проекта, их шаги и префабы-носители
    /// </summary>
    public sealed class SequenceSystemEditorWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Sequences In Project";
        private const float SQUARE = EditorToolsConstraints.BASE_ELEMENT_HEIGHT;

        private sealed class SequenceInfo
        {
            public Sequence Asset;
            public string Guid;
        }

        private static readonly Color COLOR_OPEN = new(1f, 0.52f, 0.05f);

        private readonly List<SequenceInfo> sequences = new();
        private readonly HashSet<string> expanded = new();

        private readonly HashSet<int> inspectorExpanded = new();
        private readonly Dictionary<int, UnityEditor.Editor> embeddedEditors = new();

        private Vector2 scroll;

        private GUIStyle styleMainButton;
        private GUIStyle styleGroupHeader;
        private GUIStyle styleSmall;

        [MenuItem("Diorama Enigma/" + WINDOW_NAME, priority = 100)]
        public static void Open() => GetWindow<SequenceSystemEditorWindow>(WINDOW_NAME).minSize = new Vector2(420f, 320f);

        /// <summary> Открыть окно и развернуть конкретную последовательность </summary>
        public static void Open(Sequence sequence, int stepIndex = -1)
        {
            var window = GetWindow<SequenceSystemEditorWindow>(WINDOW_NAME);
            window.minSize = new Vector2(420f, 320f);
            window.Refresh();

            if (sequence == null) return;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sequence));
            window.expanded.Add(guid);

            if (stepIndex >= 0) EditorGUIUtility.PingObject(sequence);
            window.Repaint();
        }

        private void OnEnable() => Refresh();

        private void OnDisable() => ReleaseEmbeddedEditors();

        #region GUI

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (sequences.Count == 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorGUILayout.LabelField("  Sequence в проекте не найдены", styleSmall);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var info in sequences)
                DrawSequence(info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(SQUARE));

            EditorGUILayout.LabelField($"Последовательностей: {sequences.Count}");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Объекты сцены ↗", EditorStyles.toolbarButton, GUILayout.Width(120)))
                SequenceSceneObjectsWindow.Open();

            if (GUILayout.Button("↺ Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSequence(SequenceInfo info)
        {
            bool isExpanded = expanded.Contains(info.Guid);
            int id = info.Asset.GetInstanceID();
            bool inspectorOpen = inspectorExpanded.Contains(id);

            EditorGUILayout.BeginHorizontal();

            var folderIcon = EditorGUIUtility.IconContent(isExpanded ? "FolderOpened Icon" : "Folder Icon");
            folderIcon.tooltip = isExpanded ? "Свернуть шаги" : "Развернуть шаги";
            if (GUILayout.Button(folderIcon, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
            {
                if (isExpanded) expanded.Remove(info.Guid);
                else expanded.Add(info.Guid);
            }

            string label = string.IsNullOrEmpty(info.Asset.SequenceLabel) ? info.Asset.name : info.Asset.SequenceLabel;
            string indicator = inspectorOpen ? "▼" : "▶";

            SetBG(EditorToolsConstraints.COLOR_ACCENT);
            if (GUILayout.Button($"  {indicator}  {label}   ({info.Asset.Steps.Count} шагов)", styleMainButton, GUILayout.Height(SQUARE)))
                ToggleInspector(id);
            ResetBG();

            DrawOpenButton(info.Asset);
            DrawPingButton(info.Asset);

            EditorGUILayout.EndHorizontal();

            if (inspectorOpen) DrawInlineInspector(info.Asset, 1);

            if (isExpanded) DrawSteps(info.Asset);
        }

        private void DrawSteps(Sequence sequence)
        {
            EditorGUI.indentLevel++;

            int currentGroup = -1;
            foreach (var entry in sequence.Steps)
            {
                if (entry == null) continue;

                if (entry.GroupIndex != currentGroup)
                {
                    currentGroup = entry.GroupIndex;
                    EditorGUILayout.LabelField($"   Группа {currentGroup}", styleGroupHeader);
                }

                DrawStepRow(entry.Step as ScriptableObject);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        private void DrawStepRow(ScriptableObject stepAsset)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 12f);

            if (stepAsset == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("○ (шаг не назначен)", styleMainButton, GUILayout.Height(SQUARE));
                EditorGUILayout.EndHorizontal();
                return;
            }

            int id = stepAsset.GetInstanceID();
            bool inspectorOpen = inspectorExpanded.Contains(id);
            string indicator = inspectorOpen ? "▾" : "▸";
            string label = BuildStepLabel(stepAsset);

            if (GUILayout.Button($"  {indicator}{label}", styleMainButton, GUILayout.Height(SQUARE)))
                ToggleInspector(id);

            DrawOpenButton(stepAsset);
            DrawPingButton(stepAsset);

            EditorGUILayout.EndHorizontal();

            if (inspectorOpen) DrawInlineInspector(stepAsset, EditorGUI.indentLevel + 1);
        }

        private void DrawOpenButton(Object asset)
        {
            SetBG(COLOR_OPEN);
            var content = EditorGUIUtility.IconContent("UnityEditor.InspectorWindow");
            content.tooltip = "Открыть ассет (выделить в проекте)";
            if (GUILayout.Button(content, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                Selection.activeObject = asset;
            ResetBG();
        }

        private void DrawPingButton(Object asset)
        {
            SetBG(EditorToolsConstraints.COLOR_CYAN);
            if (GUILayout.Button("●", GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                EditorGUIUtility.PingObject(asset);
            ResetBG();
        }

        /// <summary> Inline-инспектор ассета прямо под его строкой </summary>
        private void DrawInlineInspector(Object asset, int indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 12f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var editor = GetEmbeddedEditor(asset);
                if (editor != null) editor.OnInspectorGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Build

        private void Refresh()
        {
            sequences.Clear();

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Sequence)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Sequence>(path);
                if (asset == null) continue;

                sequences.Add(new SequenceInfo { Asset = asset, Guid = guid });
            }

            sequences.Sort((a, b) => string.Compare(a.Asset.name, b.Asset.name, System.StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Helpers

        private static string BuildStepLabel(ScriptableObject stepAsset)
        {
            string typeName = stepAsset.GetType().Name.Replace("Sequence", "");
            string stepLabel = stepAsset is ISequenceStep step && !string.IsNullOrEmpty(step.StepLabel)
                ? step.StepLabel
                : stepAsset.name;

            return $"  {typeName} · {stepLabel}";
        }

        private void EnsureStyles()
        {
            styleMainButton ??= new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                padding = new RectOffset(8, 6, 2, 2),
            };

            styleGroupHeader ??= new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1,
            };

            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        }

        private static void SetBG(Color color) => GUI.backgroundColor = color;
        private static void ResetBG() => GUI.backgroundColor = Color.white;

        private void ToggleInspector(int instanceId)
        {
            if (!inspectorExpanded.Remove(instanceId))
                inspectorExpanded.Add(instanceId);
        }

        /// <summary> Получить (или создать) встроенный редактор ассета; кэшируется по InstanceID </summary>
        private UnityEditor.Editor GetEmbeddedEditor(Object asset)
        {
            int id = asset.GetInstanceID();

            if (embeddedEditors.TryGetValue(id, out var editor) && editor != null && editor.target == asset)
                return editor;

            if (editor != null) DestroyImmediate(editor);

            editor = UnityEditor.Editor.CreateEditor(asset);
            embeddedEditors[id] = editor;
            return editor;
        }

        private void ReleaseEmbeddedEditors()
        {
            foreach (var editor in embeddedEditors.Values)
                if (editor != null) DestroyImmediate(editor);

            embeddedEditors.Clear();
        }

        #endregion
    }
}
