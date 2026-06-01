using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Окно редактора системы загадок. Открыть через меню Diorama Enigma → Riddle System Editor.
    /// </summary>
    public sealed class RiddleSystemEditorWindow : EditorWindow
    {
        private const float LEFT_PANEL_WIDTH = 230f;
        private const float SCENE_PANEL_HEIGHT = 150f;

        // ─── State ────────────────────────────────────────────────────────────

        private PuzzleSequence selectedSequence;
        private int selectedStepIndex = -1;

        private Vector2 leftScroll;
        private Vector2 rightScroll;
        private Vector2 sceneScroll;

        private readonly List<InteractableObject> sceneInteractables = new();
        private readonly List<DropZoneObject> sceneDropZones = new();
        private readonly Dictionary<string, List<int>> idToGroupsCache = new();

        private double lastSceneRefreshTime;

        // ─── Styles (lazy) ────────────────────────────────────────────────────

        private GUIStyle styleTitle;
        private GUIStyle styleSectionHeader;
        private GUIStyle styleStepNormal;
        private GUIStyle styleStepSelected;
        private GUIStyle styleSmall;
        private GUIStyle styleTag;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Diorama Enigma/Riddle System Editor", priority = 100)]
        public static void Open()
        {
            var window = GetWindow<RiddleSystemEditorWindow>("Riddle System Editor");
            window.minSize = new Vector2(700f, 500f);
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
                RebuildCache();
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

            float mainAreaHeight = position.height
                - EditorToolsConstraints.BASE_ELEMENT_HEIGHT  // toolbar
                - 1f                                          // separator
                - SCENE_PANEL_HEIGHT;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(mainAreaHeight));
            DrawLeftPanel(mainAreaHeight);
            DrawDividerV();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            DrawDividerH();
            DrawScenePanel();
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
                RebuildCache();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Создать...", EditorStyles.toolbarButton, GUILayout.Width(70)))
                CreateAsset<PuzzleSequence>("New PuzzleSequence");

            if (GUILayout.Button("↺", EditorStyles.toolbarButton, GUILayout.Width(28)))
                RefreshSceneObjects();

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

        private void DrawLeftPanel(float height)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.Height(height));

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

                    ColorLabel($"  Группа {currentGroup}", groupColor, styleSectionHeader);
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
            string label = step == null ? "(не назначен)" :
                string.IsNullOrEmpty(step.StepLabel) ? step.name : step.StepLabel;

            var style = selected ? styleStepSelected : styleStepNormal;
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = selected ? EditorToolsConstraints.COLOR_CYAN * 0.35f : Color.clear;

            if (GUILayout.Button($"  {label}", style,
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
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

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

            // Заголовок
            string title = step == null ? "—" :
                string.IsNullOrEmpty(step.StepLabel) ? step.name : step.StepLabel;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {title}  (Группа {entry.GroupIndex})", styleTitle);
            GUILayout.FlexibleSpace();
            if (step != null && GUILayout.Button("Открыть ассет", GUILayout.Width(100),
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                Selection.activeObject = step;
            EditorGUILayout.EndHorizontal();

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
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("→", GUILayout.Width(22), GUILayout.Height(18)))
                Selection.activeObject = condition;
            EditorGUILayout.EndHorizontal();

            // Ссылка на объект сцены
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
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    Selection.activeGameObject = linked.gameObject;
                    EditorGUIUtility.PingObject(linked.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }

            // Рекурсия для DelayedCondition
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
                EditorGUILayout.LabelField(
                    $"  {effect.GetType().Name.Replace("Effect", "")}  —  {effect.name}", styleSmall);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("→", GUILayout.Width(22), GUILayout.Height(18)))
                    Selection.activeObject = effect;
                EditorGUILayout.EndHorizontal();
            }
        }

        // ─── Scene Panel ──────────────────────────────────────────────────────

        private void DrawScenePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(SCENE_PANEL_HEIGHT));

            ColorLabel("  ОБЪЕКТЫ В СЦЕНЕ", Color.gray, styleSectionHeader);
            DrawDividerH(Color.gray * 0.5f);
            EditorGUILayout.Space(2);

            sceneScroll = EditorGUILayout.BeginScrollView(sceneScroll, GUILayout.ExpandHeight(true));

            if (sceneInteractables.Count == 0 && sceneDropZones.Count == 0)
            {
                EditorGUILayout.LabelField("  InteractableObject и DropZoneObject не найдены в сцене", styleSmall);
            }
            else
            {
                foreach (var obj in sceneInteractables)
                {
                    if (obj == null) continue;

                    bool linked = idToGroupsCache.TryGetValue(obj.Id, out var groups);
                    Color c = linked ? EditorToolsConstraints.COLOR_LIGHT_GREEN : Color.gray;

                    string groupInfo = linked ? $"→ Гр. {string.Join(",", groups)}" : string.Empty;
                    string playInfo = Application.isPlaying
                        ? $"  state:{obj.State.Current} {(obj.IsLocked ? "🔒" : "🔓")}"
                        : string.Empty;

                    EditorGUILayout.BeginHorizontal();
                    ColorLabel($"  ● {obj.gameObject.name}{playInfo}  {groupInfo}", c, styleSmall);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(16)))
                    {
                        Selection.activeGameObject = obj.gameObject;
                        EditorGUIUtility.PingObject(obj.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                foreach (var zone in sceneDropZones)
                {
                    if (zone == null) continue;

                    bool linked = IsDropZoneLinked(zone);
                    Color c = linked ? EditorToolsConstraints.COLOR_YELLOW : Color.gray;

                    EditorGUILayout.BeginHorizontal();
                    ColorLabel($"  ◈ {zone.gameObject.name}  (drop zone)", c, styleSmall);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(16)))
                    {
                        Selection.activeGameObject = zone.gameObject;
                        EditorGUIUtility.PingObject(zone.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
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
            _ => c.name
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

        // ─── Scene / Cache ────────────────────────────────────────────────────

        private void RefreshSceneObjects()
        {
            lastSceneRefreshTime = EditorApplication.timeSinceStartup;
            sceneInteractables.Clear();
            sceneDropZones.Clear();

#pragma warning disable CS0618
            sceneInteractables.AddRange(FindObjectsOfType<InteractableObject>());
            sceneDropZones.AddRange(FindObjectsOfType<DropZoneObject>());
#pragma warning restore CS0618

            RebuildCache();
            Repaint();
        }

        private void RebuildCache()
        {
            idToGroupsCache.Clear();
            if (selectedSequence == null) return;

            foreach (var entry in selectedSequence.Steps)
            {
                if (entry.Step == null) continue;

                foreach (var cond in entry.Step.Conditions)
                {
                    string id = ExtractId(cond);
                    if (id == null) continue;

                    if (!idToGroupsCache.TryGetValue(id, out var list))
                    {
                        list = new List<int>();
                        idToGroupsCache[id] = list;
                    }

                    if (!list.Contains(entry.GroupIndex))
                        list.Add(entry.GroupIndex);
                }
            }
        }

        private static string ExtractId(PuzzleCondition c) => c switch
        {
            DelayedCondition d => ExtractId(d.Inner),
            ClickCondition cc => cc.TargetId?.Id,
            DragCondition d => d.DraggableId?.Id,
            _ => null
        };

        private bool IsDropZoneLinked(DropZoneObject zone)
        {
            if (selectedSequence == null) return false;

            foreach (var entry in selectedSequence.Steps)
            {
                if (entry.Step == null) continue;
                foreach (var cond in entry.Step.Conditions)
                    if (cond is DragCondition drag && drag.DropZoneId?.Id == zone.ZoneId)
                        return true;
            }

            return false;
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
            newEntry.FindPropertyRelative("Step").objectReferenceValue = null;
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
            var rect = GUILayoutUtility.GetRect(1f, float.MaxValue, 1f, float.MaxValue);
            rect.width = 1f;
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
        }

        // ─── Styles ───────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            styleTitle ??= new GUIStyle(EditorStyles.boldLabel)
                { fontSize = EditorToolsConstraints.BASE_FONT_SIZE + 1, padding = new RectOffset(EditorToolsConstraints.TEXT_PADDING, 0, 4, 0) };

            styleSectionHeader ??= new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1 };

            styleStepNormal ??= new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleLeft, fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1 };

            styleStepSelected ??= new GUIStyle(EditorStyles.boldLabel)
                { alignment = TextAnchor.MiddleLeft, fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1 };

            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

            styleTag ??= new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, fixedWidth = 90f };
        }
    }
}
