using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Навигатор загадок: все PuzzleSequence проекта, их шаги и префабы-носители
    /// </summary>
    public sealed class RiddleSystemEditorWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Puzzle Sequences In Project";
        private const float SQUARE = EditorToolsConstraints.BASE_ELEMENT_HEIGHT;

        private sealed class SequenceInfo
        {
            public PuzzleSequence Asset;
            public string Guid;
        }

        private readonly List<SequenceInfo> sequences = new();
        private readonly HashSet<string> expanded = new();

        private Vector2 scroll;

        private GUIStyle styleMainButton;
        private GUIStyle styleGroupHeader;
        private GUIStyle styleSmall;

        [MenuItem("Diorama Enigma/" + WINDOW_NAME, priority = 100)]
        public static void Open() => GetWindow<RiddleSystemEditorWindow>(WINDOW_NAME).minSize = new Vector2(420f, 320f);

        /// <summary> Открыть окно и развернуть конкретную последовательность </summary>
        public static void Open(PuzzleSequence sequence, int stepIndex = -1)
        {
            var window = GetWindow<RiddleSystemEditorWindow>(WINDOW_NAME);
            window.minSize = new Vector2(420f, 320f);
            window.Refresh();

            if (sequence == null) return;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sequence));
            window.expanded.Add(guid);

            if (stepIndex >= 0) EditorGUIUtility.PingObject(sequence);
            window.Repaint();
        }

        private void OnEnable() => Refresh();

        #region GUI

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (sequences.Count == 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorGUILayout.LabelField("  PuzzleSequence в проекте не найдены", styleSmall);
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
                RiddleSceneObjectsWindow.Open();

            if (GUILayout.Button("↺ Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSequence(SequenceInfo info)
        {
            bool isExpanded = expanded.Contains(info.Guid);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
            {
                if (isExpanded) expanded.Remove(info.Guid);
                else expanded.Add(info.Guid);
            }

            string label = string.IsNullOrEmpty(info.Asset.SequenceLabel) ? info.Asset.name : info.Asset.SequenceLabel;

            SetBG(EditorToolsConstraints.COLOR_ACCENT);
            if (GUILayout.Button($"  {label}   ({info.Asset.Steps.Count} шагов)", styleMainButton, GUILayout.Height(SQUARE)))
                Selection.activeObject = info.Asset;
            ResetBG();

            DrawPingButton(info.Asset);

            EditorGUILayout.EndHorizontal();

            if (isExpanded) DrawSteps(info.Asset);
        }

        private void DrawSteps(PuzzleSequence sequence)
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

            string label = BuildStepLabel(stepAsset);

            if (GUILayout.Button(label, styleMainButton, GUILayout.Height(SQUARE)))
                Selection.activeObject = stepAsset;

            DrawPingButton(stepAsset);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPingButton(Object asset)
        {
            SetBG(EditorToolsConstraints.COLOR_CYAN);
            if (GUILayout.Button("●", GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                EditorGUIUtility.PingObject(asset);
            ResetBG();
        }

        #endregion

        #region Build

        private void Refresh()
        {
            sequences.Clear();

            foreach (var guid in AssetDatabase.FindAssets("t:PuzzleSequence"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<PuzzleSequence>(path);
                if (asset == null) continue;

                sequences.Add(new SequenceInfo { Asset = asset, Guid = guid });
            }

            sequences.Sort((a, b) => string.Compare(a.Asset.name, b.Asset.name, System.StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Helpers

        private static string BuildStepLabel(ScriptableObject stepAsset)
        {
            string typeName = stepAsset.GetType().Name.Replace("PuzzleStep", "");
            string stepLabel = stepAsset is IPuzzleStep step && !string.IsNullOrEmpty(step.StepLabel)
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

        #endregion
    }
}
