using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Окно обзора последовательности загадки
    /// </summary>
    public sealed class RiddleSystemEditorWindow : EditorWindow
    {
        private const float LEFT_PANEL_WIDTH = 230f;

        // ─── State ────────────────────────────────────────────────────────────

        private PuzzleSequence selectedSequence;
        private int selectedStepIndex = -1;

        private Vector2 leftScroll;
        private Vector2 rightScroll;

        private readonly List<InteractableObject> sceneInteractables = new();
        private double lastSceneRefreshTime;

        // ─── Styles (lazy) ────────────────────────────────────────────────────

        private GUIStyle styleTitle;
        private GUIStyle styleSectionHeader;
        private GUIStyle styleStepNormal;
        private GUIStyle styleStepSelected;
        private GUIStyle styleGroupHeader;
        private GUIStyle styleSmall;
        private GUIStyle styleTag;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Diorama Enigma/Riddle System Editor", priority = 100)]
        public static void Open()
        {
            var window = GetWindow<RiddleSystemEditorWindow>("Riddle System Editor");
            window.minSize = new Vector2(640f, 420f);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshSceneObjects();
        }

        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastSceneRefreshTime > 2.0)
                RefreshSceneObjects();

            if (Application.isPlaying)
                Repaint();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is PuzzleSequence seq)
            {
                selectedSequence = seq;
                selectedStepIndex = -1;
                Repaint();
            }
        }

        // ─── Main ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (selectedSequence == null)
            {
                DrawEmptyState();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawDividerV();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ─── Toolbar ──────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar,
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT));

            EditorGUILayout.LabelField("Последовательность:", GUILayout.Width(140));

            var next = (PuzzleSequence)EditorGUILayout.ObjectField(
                selectedSequence, typeof(PuzzleSequence), allowSceneObjects: false, GUILayout.Width(200));

            if (next != selectedSequence)
            {
                selectedSequence = next;
                selectedStepIndex = -1;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Объекты сцены ↗", EditorStyles.toolbarButton, GUILayout.Width(120)))
                RiddleSceneObjectsWindow.Open();

            if (GUILayout.Button("Создать...", EditorStyles.toolbarButton, GUILayout.Width(70)))
                CreateAsset<PuzzleSequence>("New PuzzleSequence");

            EditorGUILayout.EndHorizontal();

            DrawDividerH();
        }

        // ─── Empty State ──────────────────────────────────────────────────────

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Выберите PuzzleSequence в окне Project", styleTitle);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            if (GUILayout.Button("Создать новую PuzzleSequence",
                GUILayout.Width(260), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                CreateAsset<PuzzleSequence>("New PuzzleSequence");

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ─── Left Panel ───────────────────────────────────────────────────────

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));

            ColorLabel("  ШАГИ", EditorToolsConstraints.COLOR_CYAN, styleSectionHeader);
            DrawDividerH(EditorToolsConstraints.COLOR_CYAN * 0.5f);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUIStyle.none, GUI.skin.verticalScrollbar);

            int globalIndex = 0;
            int currentGroup = -1;

            foreach (var entry in selectedSequence.Steps)
            {
                if (entry.GroupIndex != currentGroup)
                {
                    currentGroup = entry.GroupIndex;

                    if (globalIndex > 0) EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

                    Color groupColor = currentGroup % 2 == 0
                        ? EditorToolsConstraints.COLOR_CYAN
                        : EditorToolsConstraints.COLOR_GREEN;

                    ColorLabel($"Группа {currentGroup}", groupColor, styleGroupHeader);
                }

                DrawStepRow(entry.Step, globalIndex);
                globalIndex++;
            }

            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            if (GUILayout.Button("+ Шаг (следующая группа)",
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                AddStep();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawStepRow(PuzzleStep step, int index)
        {
            bool selected = index == selectedStepIndex;
            string label = step == null ? "○ (не назначен)" :
                string.IsNullOrEmpty(step.StepLabel) ? $"Шаг {index}" : step.StepLabel;

            var prevBg = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = EditorToolsConstraints.COLOR_CYAN;

            if (GUILayout.Button(label, selected ? styleStepSelected : styleStepNormal,
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                selectedStepIndex = index;
                GUI.FocusControl(null);
            }

            GUI.backgroundColor = prevBg;
        }

        // ─── Right Panel ──────────────────────────────────────────────────────

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (selectedStepIndex < 0 || selectedStepIndex >= selectedSequence.Steps.Count)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("  Выберите шаг слева", styleTitle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            var entry = selectedSequence.Steps[selectedStepIndex];
            var step = entry.Step;

            string title = step == null ? "—" :
                string.IsNullOrEmpty(step.StepLabel) ? $"Шаг {selectedStepIndex}" : step.StepLabel;

            EditorGUILayout.LabelField($"  {title}  (Группа {entry.GroupIndex})", styleTitle);
            DrawDividerH();
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            if (step == null)
            {
                EditorGUILayout.HelpBox("Шаг не назначен.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            DrawConditionsBlock(step);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            DrawEffectsBlock("▶  ПРИ АКТИВАЦИИ", step.ActivationEffects, EditorToolsConstraints.COLOR_YELLOW);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            DrawEffectsBlock("✓  ПРИ ЗАВЕРШЕНИИ", step.Effects, EditorToolsConstraints.COLOR_GREEN);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            DrawEffectsBlock("✕  ПРИ ПРОВАЛЕ", step.FailureEffects, EditorToolsConstraints.COLOR_RED);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawConditionsBlock(PuzzleStep step)
        {
            ColorLabel("  УСЛОВИЯ", EditorToolsConstraints.COLOR_CYAN, styleSectionHeader);
            DrawDividerH(EditorToolsConstraints.COLOR_CYAN * 0.4f);
            EditorGUILayout.Space(2);

            if (step.Conditions.Count == 0)
            {
                EditorGUILayout.LabelField("  (нет — шаг завершается мгновенно при активации)", styleSmall);
                return;
            }

            foreach (var cond in step.Conditions)
            {
                if (cond == null) { EditorGUILayout.LabelField("  ○ null", styleSmall); continue; }
                DrawConditionRow(cond);
            }
        }

        private void DrawConditionRow(PuzzleCondition condition)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            Color tagColor = GetConditionColor(condition);
            ColorLabel($"[{GetConditionTypeName(condition)}]", tagColor, styleTag);
            EditorGUILayout.LabelField(GetConditionSummary(condition), styleSmall);
            EditorGUILayout.EndHorizontal();

            var linked = FindLinkedObject(condition);
            if (linked != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                string playInfo = Application.isPlaying
                    ? $"  state:{linked.State.Current} {(linked.IsLocked ? "🔒" : "🔓")}"
                    : string.Empty;
                ColorLabel($"→ {linked.gameObject.name} (сцена){playInfo}",
                    EditorToolsConstraints.COLOR_LIGHT_GREEN, styleSmall);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", GUILayout.Width(44), GUILayout.Height(16)))
                {
                    Selection.activeGameObject = linked.gameObject;
                    EditorGUIUtility.PingObject(linked.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (condition is DelayedCondition delayed && delayed.Inner != null)
            {
                EditorGUI.indentLevel++;
                DrawConditionRow(delayed.Inner);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEffectsBlock(string title, IReadOnlyList<PuzzleEffect> effects, Color color)
        {
            ColorLabel($"  {title}", color, styleSectionHeader);
            DrawDividerH(color * 0.4f);
            EditorGUILayout.Space(2);

            if (effects == null || effects.Count == 0)
            {
                EditorGUILayout.LabelField("  (нет)", styleSmall);
                return;
            }

            foreach (var effect in effects)
            {
                if (effect == null) { EditorGUILayout.LabelField("  ○ null", styleSmall); continue; }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"  {effect.GetType().Name.Replace("Effect", "")}", styleSmall);
                EditorGUILayout.EndHorizontal();
            }
        }

        // ─── Condition Helpers ────────────────────────────────────────────────

        private static string GetConditionTypeName(PuzzleCondition c) => c switch
        {
            DelayedCondition d => $"Delay +{d.DelaySeconds:0.##}s",
            ClickCondition => "Click",
            DragCondition => "Drag",
            ResourceCondition => "Resource",
            _ => c.GetType().Name.Replace("Condition", "")
        };

        private static string GetConditionSummary(PuzzleCondition c) => c switch
        {
            DelayedCondition d => d.Inner != null ? GetConditionSummary(d.Inner) : "?",
            ClickCondition cc => $"{(cc.TargetId != null ? cc.TargetId.name : "?")} → state {cc.RequiredStateIndex}",
            DragCondition d => $"{(d.DraggableId != null ? d.DraggableId.name : "?")} → {(d.DropZoneId != null ? d.DropZoneId.name : "?")}",
            ResourceCondition r => $"{(r.Resource != null ? r.Resource.name : "?")} == {r.RequiredValue}",
            _ => c.GetType().Name
        };

        private static Color GetConditionColor(PuzzleCondition c) => c switch
        {
            DelayedCondition => EditorToolsConstraints.COLOR_PURPLE,
            ClickCondition => EditorToolsConstraints.COLOR_CYAN,
            DragCondition => EditorToolsConstraints.COLOR_YELLOW,
            ResourceCondition => EditorToolsConstraints.COLOR_GREEN,
            _ => Color.white
        };

        private InteractableObject FindLinkedObject(PuzzleCondition condition)
        {
            string id = condition switch
            {
                DelayedCondition d when d.Inner is ClickCondition c => c.TargetId?.Id,
                ClickCondition c => c.TargetId?.Id,
                DragCondition d => d.DraggableId?.Id,
                _ => null
            };

            if (id == null) return null;

            foreach (var obj in sceneInteractables)
                if (obj != null && obj.Id == id) return obj;

            return null;
        }

        // ─── Scene ────────────────────────────────────────────────────────────

        private void RefreshSceneObjects()
        {
            lastSceneRefreshTime = EditorApplication.timeSinceStartup;
            sceneInteractables.Clear();

#pragma warning disable CS0618
            sceneInteractables.AddRange(FindObjectsOfType<InteractableObject>());
#pragma warning restore CS0618
        }

        // ─── Asset Creation ───────────────────────────────────────────────────

        private void AddStep()
        {
            if (selectedSequence == null) return;

            var so = new SerializedObject(selectedSequence);
            var stepsProp = so.FindProperty("steps");
            int nextGroup = 0;

            if (stepsProp.arraySize > 0)
            {
                var last = stepsProp.GetArrayElementAtIndex(stepsProp.arraySize - 1);
                nextGroup = last.FindPropertyRelative("GroupIndex").intValue + 1;
            }

            stepsProp.arraySize++;
            var newEntry = stepsProp.GetArrayElementAtIndex(stepsProp.arraySize - 1);
            newEntry.FindPropertyRelative("Step").managedReferenceValue = new PuzzleStep();
            newEntry.FindPropertyRelative("GroupIndex").intValue = nextGroup;
            so.ApplyModifiedProperties();

            selectedStepIndex = stepsProp.arraySize - 1;
        }

        private static void CreateAsset<T>(string defaultName) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"Создать {typeof(T).Name}", defaultName, "asset", string.Empty);

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }

        // ─── Drawing Helpers ──────────────────────────────────────────────────

        private static void ColorLabel(string text, Color color, GUIStyle style)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(text, style);
            GUI.contentColor = prev;
        }

        private static void DrawDividerH(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, color ?? new Color(0.15f, 0.15f, 0.15f, 1f));
        }

        private static void DrawDividerV()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
        }

        // ─── Styles ───────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            styleTitle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE + 1,
                padding = new RectOffset(EditorToolsConstraints.TEXT_PADDING, 0, 4, 0)
            };

            styleSectionHeader ??= new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1 };

            styleGroupHeader ??= new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1,
                padding = new RectOffset(2, 0, 2, 2)
            };

            styleStepNormal ??= new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                padding = new RectOffset(10, 6, 2, 2)
            };

            styleStepSelected ??= new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                padding = new RectOffset(10, 6, 2, 2)
            };

            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

            styleTag ??= new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, fixedWidth = 90f };
        }
    }
}
