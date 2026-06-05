using System.Collections.Generic;
using System;
using Extensions.EditorTools;
using Extensions.ScriptableValues;
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
        private const double SCENE_REFRESH_INTERVAL = 2.0;

        private const string STATE_PROP = "state";
        private const string GROUP_INDEX_PROP = "groupIndex";

        private PuzzleSequence selectedSequence;
        private int selectedStepIndex = -1;

        private Vector2 leftScroll;
        private Vector2 rightScroll;

        private struct InputLink
        {
            public MonoBehaviour Component;
            public ScriptableObject StateAsset;
        }

        private readonly List<InputLink> sceneInputLinks = new();
        private double lastSceneRefreshTime;

        private GUIStyle styleTitle;
        private GUIStyle styleSectionHeader;
        private GUIStyle styleStepNormal;
        private GUIStyle styleStepSelected;
        private GUIStyle styleGroupHeader;
        private GUIStyle styleSmall;

        [MenuItem("Diorama Enigma/Riddle System Editor", priority = 100)]
        public static void Open()
        {
            var window = GetWindow<RiddleSystemEditorWindow>("Riddle System Editor");
            window.minSize = new Vector2(640f, 420f);
        }

        /// <summary> Открыть окно и перейти к конкретному шагу </summary>
        public static void Open(PuzzleSequence sequence, int stepIndex = -1)
        {
            var window = GetWindow<RiddleSystemEditorWindow>("Riddle System Editor");
            window.minSize = new Vector2(640f, 420f);
            if (sequence == null) return;
            window.selectedSequence = sequence;
            window.selectedStepIndex = stepIndex;
            window.Repaint();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshSceneObjects();
        }

        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastSceneRefreshTime > SCENE_REFRESH_INTERVAL)
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

        #region Main

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

        #endregion

        #region Toolbar

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

        #endregion

        #region Empty state

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

        #endregion

        #region Left panel

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

        private void DrawStepRow(IPuzzleStep step, int index)
        {
            bool selected = index == selectedStepIndex;

            string label = step == null ? "○ (не назначен)" :
                string.IsNullOrEmpty(step.StepLabel) ? $"Шаг {index}" : step.StepLabel;

            if (Application.isPlaying && step != null)
                label = (step.IsCompleted ? "✓ " : "● ") + label;

            var prevBg = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = EditorToolsConstraints.COLOR_CYAN;

            if (GUILayout.Button(label, selected ? styleStepSelected : styleStepNormal,
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                selectedStepIndex = index;
                GUI.FocusControl(null);
            }

            var rowRect = GUILayoutUtility.GetLastRect();
            GUI.backgroundColor = prevBg;

            if (step != null) HandleStepRowDrag(rowRect, step);
        }

        #endregion

        #region Right panel

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
                EditorGUILayout.HelpBox("Шаг не назначен. Назначьте ассет-шаг в инспекторе PuzzleSequence.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            DrawStepInfoBlock(step);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            DrawLinkedInputsSection(step);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            DrawEffectsBlock("▶  ПРИ АКТИВАЦИИ", entry.ActivationEffects, EditorToolsConstraints.COLOR_YELLOW);
            EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
            DrawEffectsBlock("✓  ПРИ ЗАВЕРШЕНИИ", entry.CompletionEffects, EditorToolsConstraints.COLOR_GREEN);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawStepInfoBlock(IPuzzleStep step)
        {
            ColorLabel("  ШАГ", EditorToolsConstraints.COLOR_CYAN, styleSectionHeader);
            DrawDividerH(EditorToolsConstraints.COLOR_CYAN * 0.4f);
            EditorGUILayout.Space(2);

            EditorGUILayout.LabelField($"Тип: {step.GetType().Name}", styleSmall);

            if (Application.isPlaying)
            {
                bool completed = step.IsCompleted;
                string completedStr = completed ? "✓ завершён" : "● ожидание";
                string valueStr = GetStepValueString(step);

                Color prevColor = GUI.contentColor;
                GUI.contentColor = completed ? EditorToolsConstraints.COLOR_GREEN : EditorToolsConstraints.COLOR_YELLOW;
                EditorGUILayout.LabelField($"  {completedStr}  {valueStr}", styleSmall);
                GUI.contentColor = prevColor;
            }

            DrawGateInfo(step);
        }

        private void DrawGateInfo(IPuzzleStep step)
        {
            var stepAsset = step as ScriptableObject;
            if (stepAsset == null) return;

            var so = new SerializedObject(stepAsset);
            var trackerProp = so.FindProperty("tracker");
            if (trackerProp == null) return;

            var gateProp = trackerProp.FindPropertyRelative("gate");
            if (gateProp == null) return;

            EditorGUILayout.Space(2);

            bool hasGate = !string.IsNullOrEmpty(gateProp.managedReferenceFullTypename);
            string gateStr = hasGate ? GateSummary(gateProp) : "(нет)";
            EditorGUILayout.LabelField($"Гейт: {gateStr}", styleSmall);
        }

        private static string GetStepValueString(IPuzzleStep step) => step switch
        {
            StateSetPuzzleStep intStep => $"val:{intStep.Value}",
            BoolPuzzleStep boolStep => $"val:{boolStep.Value}",
            StringPuzzleStep strStep => $"val:\"{strStep.Value}\"",
            _ => string.Empty,
        };

        private static string GateSummary(SerializedProperty gateProp)
        {
            string typeName = ManagedRefTypeName(gateProp);
            var requiredProp = gateProp.FindPropertyRelative("requiredSteps");
            if (requiredProp != null) return $"{typeName} [{requiredProp.arraySize} шагов]";
            return typeName;
        }

        private static string ManagedRefTypeName(SerializedProperty prop)
        {
            string full = prop.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return string.Empty;
            int dot = full.LastIndexOf('.');
            return dot >= 0 ? full[(dot + 1)..] : full;
        }

        private void DrawLinkedInputsSection(IPuzzleStep step)
        {
            ColorLabel("  СВЯЗАННЫЕ ОБЪЕКТЫ (сцена)", EditorToolsConstraints.COLOR_LIGHT_GREEN, styleSectionHeader);
            DrawDividerH(EditorToolsConstraints.COLOR_LIGHT_GREEN * 0.4f);
            EditorGUILayout.Space(2);

            var stepAsset = step as ScriptableObject;
            bool found = false;

            if (stepAsset != null)
            {
                foreach (var link in sceneInputLinks)
                {
                    if (link.StateAsset != stepAsset || link.Component == null) continue;

                    found = true;
                    string typeName = link.Component.GetType().Name.Replace("Interactable", "");

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    ColorLabel($"→ {link.Component.gameObject.name}  [{typeName}]",
                        EditorToolsConstraints.COLOR_LIGHT_GREEN, styleSmall);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ping", GUILayout.Width(44), GUILayout.Height(16)))
                    {
                        Selection.activeGameObject = link.Component.gameObject;
                        EditorGUIUtility.PingObject(link.Component.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (!found)
                EditorGUILayout.LabelField("  (нет связанных объектов в сцене)", styleSmall);
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

        #endregion

        #region Scene

        private void RefreshSceneObjects()
        {
            lastSceneRefreshTime = EditorApplication.timeSinceStartup;
            sceneInputLinks.Clear();

            foreach (var click in FindObjectsByType<ClickInteractable>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(click);
                var stateRef = so.FindProperty(STATE_PROP)?.objectReferenceValue as ScriptableObject;
                sceneInputLinks.Add(new InputLink { Component = click, StateAsset = stateRef });
            }

            foreach (var drag in FindObjectsByType<DraggableInteractable>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(drag);
                var stateRef = so.FindProperty(STATE_PROP)?.objectReferenceValue as ScriptableObject;
                sceneInputLinks.Add(new InputLink { Component = drag, StateAsset = stateRef });
            }
        }

        #endregion

        #region Asset creation

        private void AddStep()
        {
            if (selectedSequence == null) return;

            var so = new SerializedObject(selectedSequence);
            var stepsProp = so.FindProperty("steps");
            int nextGroup = 0;

            if (stepsProp.arraySize > 0)
            {
                var last = stepsProp.GetArrayElementAtIndex(stepsProp.arraySize - 1);
                nextGroup = last.FindPropertyRelative(GROUP_INDEX_PROP).intValue + 1;
            }

            stepsProp.arraySize++;
            var newEntry = stepsProp.GetArrayElementAtIndex(stepsProp.arraySize - 1);
            newEntry.FindPropertyRelative("step").objectReferenceValue = null;
            newEntry.FindPropertyRelative(GROUP_INDEX_PROP).intValue = nextGroup;
            newEntry.FindPropertyRelative("activationEffects").ClearArray();
            newEntry.FindPropertyRelative("completionEffects").ClearArray();
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

        #endregion

        #region Drawing helpers

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

        #endregion

        #region Drag-drop

        private void HandleStepRowDrag(Rect rowRect, IPuzzleStep step)
        {
            var evt = Event.current;
            if (!rowRect.Contains(evt.mousePosition)) return;

            var stepAsset = step as ScriptableObject;
            if (stepAsset == null) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                {
                    var comp = GetDraggedInteractable();
                    if (comp != null && CanAssign(stepAsset, comp))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        evt.Use();
                    }
                    break;
                }
                case EventType.DragPerform:
                {
                    var comp = GetDraggedInteractable();
                    if (comp != null && CanAssign(stepAsset, comp))
                    {
                        DragAndDrop.AcceptDrag();
                        AssignStepToInteractable(stepAsset, comp);
                        RefreshSceneObjects();
                        evt.Use();
                    }
                    break;
                }
                case EventType.Repaint:
                {
                    if (DragAndDrop.visualMode == DragAndDropVisualMode.Link)
                    {
                        var comp = GetDraggedInteractable();
                        if (comp != null && CanAssign(stepAsset, comp))
                            EditorGUI.DrawRect(rowRect, new Color(0.3f, 0.8f, 0.3f, 0.25f));
                    }
                    break;
                }
            }
        }

        private static MonoBehaviour GetDraggedInteractable()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is not GameObject go) continue;
                var click = go.GetComponent<ClickInteractable>();
                if (click != null) return click;
                var drag = go.GetComponent<DraggableInteractable>();
                if (drag != null) return drag;
            }
            return null;
        }

        private static bool CanAssign(ScriptableObject stepAsset, MonoBehaviour interactable)
        {
            return (interactable is ClickInteractable && stepAsset is IntValue)
                || (interactable is DraggableInteractable && stepAsset is BoolValue);
        }

        private static void AssignStepToInteractable(ScriptableObject stepAsset, MonoBehaviour interactable)
        {
            var so = new SerializedObject(interactable);
            var stateProp = so.FindProperty(STATE_PROP);
            if (stateProp == null) return;
            stateProp.objectReferenceValue = stepAsset;
            so.ApplyModifiedProperties();
        }

        #endregion

        #region Styles

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
        }

        #endregion
    }
}
