using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Инспектор PuzzleSequence: редактирование записей шагов (ссылки на ассеты + эффекты)
    /// </summary>
    [CustomEditor(typeof(PuzzleSequence))]
    public sealed class PuzzleSequenceEditor : UnityEditor.Editor
    {
        private const string STEPS_FOLDER_PATH = "Assets/Features/Riddles/Data";
        private const string STEPS_FOLDER_NAME = "Steps";
            
        private SerializedProperty sequenceLabelProp;
        private SerializedProperty stepsProp;

        private GUIStyle headerStyle;

        // ─── Warnings cache ───────────────────────────────────────────────────
        private readonly HashSet<int> disconnectedSteps = new();
        private readonly Dictionary<int, List<string>> sharedSteps = new();
        private bool warningsBuilt;

        // ─── Step SO cache ────────────────────────────────────────────────────
        // Кешируем SerializedObject шагов: Unity откладывает callback [SerializeReference]
        // пикера; если пересоздавать SO каждый кадр, ссылка устаревает до выбора типа.
        private readonly Dictionary<int, SerializedObject> stepSOCache = new();

        private static readonly Color SeparatorEven = new(0.25f, 0.45f, 0.65f, 1f);
        private static readonly Color SeparatorOdd = new(0.35f, 0.55f, 0.35f, 1f);

        private static readonly (string label, Type type)[] StepTypes =
        {
            ("Bool Step",      typeof(BoolPuzzleStep)),
            ("State Set Step", typeof(StateSetPuzzleStep)),
            ("String Step",    typeof(StringPuzzleStep)),
            ("Composite Step", typeof(CompositePuzzleStep)),
        };

        private static readonly (string label, Type type)[] EffectTypes =
        {
            ("Award Resource", typeof(AwardResourceEffect)),
            ("Fire Event",     typeof(FireEventEffect)),
        };

        private static readonly (string label, Type type)[] GateTypes =
        {
            ("Требует завершения шагов", typeof(RequireStepsCompletedGate)),
        };

        private void OnEnable()
        {
            sequenceLabelProp = serializedObject.FindProperty("sequenceLabel");
            stepsProp = serializedObject.FindProperty("steps");
            RefreshWarnings();
        }

        private void OnDisable() => stepSOCache.Clear();

        public override void OnInspectorGUI()
        {
            headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(sequenceLabelProp);
            if (GUILayout.Button("↺", GUILayout.Width(24), GUILayout.Height(18)))
                RefreshWarnings();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            var groups = GetSortedGroups();

            for (int position = 0; position < groups.Count; position++)
                DrawGroup(groups[position], position, groups);

            EditorGUILayout.Space(4);

            int maxGroup = groups.Count > 0 ? groups[^1] : -1;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Создать шаг (новая группа)"))
                ShowCreateStepMenu(maxGroup + 1);
            if (groups.Count > 0 && GUILayout.Button("+ Создать шаг (в ту же группу)"))
                ShowCreateStepMenu(maxGroup);
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        // ─── Groups ───────────────────────────────────────────────────────────

        private void DrawGroup(int groupIndex, int position, List<int> groups)
        {
            Color separatorColor = position % 2 == 0 ? SeparatorEven : SeparatorOdd;

            var rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, separatorColor);
            EditorGUILayout.Space(2);

            int count = CountStepsInGroup(groupIndex);
            string groupLabel = count > 1 ? $"Группа {position}  —  {count} шага параллельно" : $"Группа {position}";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(groupLabel, headerStyle);
            GUILayout.FlexibleSpace();

            bool moveUp = false, moveDown = false;

            using (new EditorGUI.DisabledScope(position == 0))
                if (GUILayout.Button("▲", GUILayout.Width(24))) moveUp = true;

            using (new EditorGUI.DisabledScope(position == groups.Count - 1))
                if (GUILayout.Button("▼", GUILayout.Width(24))) moveDown = true;

            EditorGUILayout.EndHorizontal();

            if (moveUp) { SwapGroups(groupIndex, groups[position - 1]); return; }
            if (moveDown) { SwapGroups(groupIndex, groups[position + 1]); return; }

            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var entry = stepsProp.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("groupIndex").intValue != groupIndex) continue;
                DrawStepEntry(entry, i);
            }

            EditorGUILayout.Space(2);
        }

        // ─── Step Entry ───────────────────────────────────────────────────────

        private void DrawStepEntry(SerializedProperty entry, int arrayIndex)
        {
            var stepProp = entry.FindPropertyRelative("step");
            bool stepIsNull = stepProp.objectReferenceValue == null;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row: step asset picker + [Создать] (when null) + move + delete
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(stepProp, GUIContent.none);

            if (stepIsNull)
            {
                // Capture for lambda; copy avoids closure-over-loop-variable
                var capturedProp = stepProp.Copy();
                if (GUILayout.Button("Создать ▾", GUILayout.Width(72)))
                    ShowCreateMenuForEntry(capturedProp);
            }

            int moveDir = 0;
            if (GUILayout.Button("▲", GUILayout.Width(20))) moveDir = -1;
            if (GUILayout.Button("▼", GUILayout.Width(20))) moveDir = +1;
            bool delete = GUILayout.Button("✕", GUILayout.Width(20));

            EditorGUILayout.EndHorizontal();

            if (delete)
            {
                stepsProp.DeleteArrayElementAtIndex(arrayIndex);
                EditorGUILayout.EndVertical();
                return;
            }

            if (moveDir != 0)
            {
                MoveStepToAdjacentGroup(arrayIndex, moveDir);
                EditorGUILayout.EndVertical();
                return;
            }

            // Step asset summary + inline gate editing
            if (stepProp.objectReferenceValue is ScriptableObject stepAsset)
            {
                if (stepAsset is IPuzzleStep puzzleStep)
                {
                    string label = string.IsNullOrEmpty(puzzleStep.StepLabel) ? "(без метки)" : puzzleStep.StepLabel;
                    string typeName = stepAsset.GetType().Name.Replace("PuzzleStep", "");
                    EditorGUILayout.LabelField($"  {typeName} · {label}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox($"{stepAsset.GetType().Name} не реализует IPuzzleStep", MessageType.Warning);
                }

                DrawStepWarnings(stepAsset.GetInstanceID());
                DrawStepGate(stepAsset);
            }

            DrawManagedRefArray(entry.FindPropertyRelative("activationEffects"), "ЭФФЕКТЫ ПРИ АКТИВАЦИИ", EffectTypes);
            DrawManagedRefArray(entry.FindPropertyRelative("completionEffects"), "ЭФФЕКТЫ ПРИ ЗАВЕРШЕНИИ", EffectTypes);

            EditorGUILayout.EndVertical();
        }

        // ─── Gate (nested SO editor) ──────────────────────────────────────────

        private void DrawStepGate(ScriptableObject stepAsset)
        {
            var so = GetStepSO(stepAsset);
            so.Update();

            var trackerProp = so.FindProperty("tracker");
            if (trackerProp == null) return;

            var gateProp = trackerProp.FindPropertyRelative("gate");
            if (gateProp == null) return;

            EditorGUILayout.Space(2);

            bool hasGate = !string.IsNullOrEmpty(gateProp.managedReferenceFullTypename);

            // ── Header row ─────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Гейт:", EditorStyles.miniLabel, GUILayout.Width(38));

            if (hasGate)
            {
                EditorGUILayout.LabelField(ManagedRefTypeName(gateProp), EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(16)))
                {
                    gateProp.managedReferenceValue = null;
                    so.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorGUILayout.LabelField("(нет)", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+ Добавить", GUILayout.Width(72), GUILayout.Height(16)))
                    ShowGateMenu(gateProp, so);
            }

            EditorGUILayout.EndHorizontal();

            // ── Gate body ──────────────────────────────────────────────────────
            if (hasGate)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                DrawManagedRefBody(gateProp);
                if (EditorGUI.EndChangeCheck())
                    so.ApplyModifiedProperties();
                EditorGUI.indentLevel--;
            }
        }

        private void ShowGateMenu(SerializedProperty gateProp, SerializedObject so)
        {
            var menu = new GenericMenu();
            foreach (var (menuLabel, type) in GateTypes)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(menuLabel), false, () =>
                {
                    so.Update();
                    gateProp.managedReferenceValue = Activator.CreateInstance(captured);
                    so.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        private SerializedObject GetStepSO(ScriptableObject stepAsset)
        {
            int id = stepAsset.GetInstanceID();
            if (!stepSOCache.TryGetValue(id, out var so) || so == null || !so.targetObject)
            {
                so = new SerializedObject(stepAsset);
                stepSOCache[id] = so;
            }
            return so;
        }

        // ─── SerializeReference array ─────────────────────────────────────────

        private void DrawManagedRefArray(SerializedProperty arrayProp, string label, (string, Type)[] addChoices)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            int removeIndex = -1;

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var el = arrayProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                bool isNull = string.IsNullOrEmpty(el.managedReferenceFullTypename);
                EditorGUILayout.LabelField(isNull ? "(null)" : ManagedRefTypeName(el), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(18))) removeIndex = i;
                EditorGUILayout.EndHorizontal();

                if (!isNull) DrawManagedRefBody(el);

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                arrayProp.DeleteArrayElementAtIndex(removeIndex);

            if (GUILayout.Button("+ Добавить", GUILayout.Height(20)))
                ShowAddMenu(arrayProp.propertyPath, addChoices);
        }

        private void DrawManagedRefBody(SerializedProperty prop)
        {
            var end = prop.GetEndProperty();
            var it = prop.Copy();
            bool enter = true;

            EditorGUI.indentLevel++;

            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = false;
                EditorGUILayout.PropertyField(it, true);
            }

            EditorGUI.indentLevel--;
        }

        // ─── Step creation menus ──────────────────────────────────────────────

        /// <summary>Меню создания шага с добавлением новой записи в последовательность</summary>
        private void ShowCreateStepMenu(int groupIndex)
        {
            var menu = new GenericMenu();

            foreach (var (menuLabel, type) in StepTypes)
            {
                Type captured = type;
                int capturedGroup = groupIndex;
                menu.AddItem(new GUIContent(menuLabel), false, () =>
                {
                    var asset = CreateStepAsset(captured);
                    if (asset == null) return;

                    serializedObject.Update();
                    int idx = stepsProp.arraySize;
                    stepsProp.arraySize++;
                    var entry = stepsProp.GetArrayElementAtIndex(idx);
                    entry.FindPropertyRelative("step").objectReferenceValue = asset;
                    entry.FindPropertyRelative("groupIndex").intValue = capturedGroup;
                    entry.FindPropertyRelative("activationEffects").ClearArray();
                    entry.FindPropertyRelative("completionEffects").ClearArray();
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Пустой слот (выбрать позже)"), false, () => AddStep(groupIndex));

            menu.ShowAsContext();
        }

        /// <summary>Меню создания шага и назначения его в существующий пустой слот</summary>
        private void ShowCreateMenuForEntry(SerializedProperty stepProp)
        {
            var menu = new GenericMenu();

            foreach (var (menuLabel, type) in StepTypes)
            {
                Type captured = type;
                SerializedProperty capturedProp = stepProp.Copy();
                menu.AddItem(new GUIContent(menuLabel), false, () =>
                {
                    var asset = CreateStepAsset(captured);
                    if (asset == null) return;

                    serializedObject.Update();
                    capturedProp.objectReferenceValue = asset;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // ─── Asset creation ───────────────────────────────────────────────────

        private ScriptableObject CreateStepAsset(Type stepType)
        {
            string folder = GetOrCreateStepFolder();
            if (folder == null) return null;

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{stepType.Name}.asset");
            var asset = ScriptableObject.CreateInstance(stepType);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private string GetOrCreateStepFolder()
        {
            var sequence = (PuzzleSequence)target;
            string label = string.IsNullOrEmpty(sequence.SequenceLabel)
                ? sequence.name
                : sequence.SequenceLabel;

            string folderName = SanitizeFolderName(label);
            if (string.IsNullOrEmpty(folderName)) folderName = "Default";

            string targetFolder = $"{STEPS_FOLDER_PATH}/{STEPS_FOLDER_NAME}/{folderName}";

            EnsureFolder($"{STEPS_FOLDER_PATH}");
            EnsureFolder($"{STEPS_FOLDER_PATH}/{STEPS_FOLDER_NAME}");
            EnsureFolder(targetFolder);

            return targetFolder;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/') ?? string.Empty;
            string name = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static string SanitizeFolderName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(Array.IndexOf(invalid, c) < 0 ? c : '_');
            return sb.ToString().Trim('_', ' ');
        }

        // ─── Effect type menu ─────────────────────────────────────────────────

        private void ShowAddMenu(string arrayPath, (string label, Type type)[] choices)
        {
            var menu = new GenericMenu();

            foreach (var (menuLabel, type) in choices)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(menuLabel), false, () =>
                {
                    serializedObject.Update();
                    var arr = serializedObject.FindProperty(arrayPath);
                    int idx = arr.arraySize;
                    arr.arraySize++;
                    arr.GetArrayElementAtIndex(idx).managedReferenceValue = Activator.CreateInstance(captured);
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private static string ManagedRefTypeName(SerializedProperty prop)
        {
            string full = prop.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return "(null)";
            int dot = full.LastIndexOf('.');
            return dot >= 0 ? full[(dot + 1)..] : full;
        }

        // ─── Step / group management ──────────────────────────────────────────

        private void AddStep(int groupIndex)
        {
            int idx = stepsProp.arraySize;
            stepsProp.arraySize++;
            var entry = stepsProp.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("step").objectReferenceValue = null;
            entry.FindPropertyRelative("groupIndex").intValue = groupIndex;
            entry.FindPropertyRelative("activationEffects").ClearArray();
            entry.FindPropertyRelative("completionEffects").ClearArray();
        }

        private List<int> GetSortedGroups()
        {
            var set = new SortedSet<int>();
            for (int i = 0; i < stepsProp.arraySize; i++)
                set.Add(stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("groupIndex").intValue);
            return new List<int>(set);
        }

        private void SwapGroups(int groupA, int groupB)
        {
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var gp = stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("groupIndex");
                if (gp.intValue == groupA) gp.intValue = groupB;
                else if (gp.intValue == groupB) gp.intValue = groupA;
            }
        }

        private void MoveStepToAdjacentGroup(int arrayIndex, int direction)
        {
            var groups = GetSortedGroups();
            var gp = stepsProp.GetArrayElementAtIndex(arrayIndex).FindPropertyRelative("groupIndex");
            int currentPos = groups.IndexOf(gp.intValue);
            int targetPos = currentPos + direction;

            if (targetPos < 0) gp.intValue = groups[0] - 1;
            else if (targetPos >= groups.Count) gp.intValue = groups[^1] + 1;
            else gp.intValue = groups[targetPos];
        }

        private int CountStepsInGroup(int groupIndex)
        {
            int count = 0;
            for (int i = 0; i < stepsProp.arraySize; i++)
                if (stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("groupIndex").intValue == groupIndex)
                    count++;
            return count;
        }

        // ─── Warnings ─────────────────────────────────────────────────────────

        private void RefreshWarnings()
        {
            disconnectedSteps.Clear();
            sharedSteps.Clear();
            warningsBuilt = false;

            // Collect step assets of this sequence that need scene connections
            var valueStepIDs = new HashSet<int>();
            var allStepIDs = new HashSet<int>();

            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var asset = stepsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("step").objectReferenceValue as ScriptableObject;
                if (asset == null) continue;

                allStepIDs.Add(asset.GetInstanceID());

                // Only value-based steps need direct scene input
                if (asset is StateSetPuzzleStep || asset is BoolPuzzleStep || asset is StringPuzzleStep)
                    valueStepIDs.Add(asset.GetInstanceID());
            }

            if (allStepIDs.Count == 0) { warningsBuilt = true; return; }

            // ── Scene connection check ──────────────────────────────────────
            var connectedIDs = new HashSet<int>();
            CollectConnectedStateIDs(connectedIDs, FindObjectsByType<ClickInteractable>(FindObjectsSortMode.None));
            CollectConnectedStateIDs(connectedIDs, FindObjectsByType<DraggableInteractable>(FindObjectsSortMode.None));

            foreach (int id in valueStepIDs)
                if (!connectedIDs.Contains(id))
                    disconnectedSteps.Add(id);

            // ── Cross-sequence sharing check ────────────────────────────────
            var guids = AssetDatabase.FindAssets("t:PuzzleSequence");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var otherSeq = AssetDatabase.LoadAssetAtPath<PuzzleSequence>(path);
                if (otherSeq == null || otherSeq == target) continue;

                var otherSO = new SerializedObject(otherSeq);
                var otherSteps = otherSO.FindProperty("steps");

                for (int i = 0; i < otherSteps.arraySize; i++)
                {
                    var otherAsset = otherSteps.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("step").objectReferenceValue as ScriptableObject;
                    if (otherAsset == null || !allStepIDs.Contains(otherAsset.GetInstanceID())) continue;

                    int id = otherAsset.GetInstanceID();
                    if (!sharedSteps.TryGetValue(id, out var names))
                        sharedSteps[id] = names = new List<string>();
                    if (!names.Contains(otherSeq.name))
                        names.Add(otherSeq.name);
                }
            }

            warningsBuilt = true;
        }

        private static void CollectConnectedStateIDs<T>(HashSet<int> ids, T[] components)
            where T : UnityEngine.Component
        {
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var stateRef = so.FindProperty("state")?.objectReferenceValue as ScriptableObject;
                if (stateRef != null) ids.Add(stateRef.GetInstanceID());
            }
        }

        private void DrawStepWarnings(int stepId)
        {
            if (!warningsBuilt) return;

            if (disconnectedSteps.Contains(stepId))
            {
                var prev = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.75f, 0.2f);
                EditorGUILayout.LabelField("  ⚠  Нет связанных объектов в сцене", EditorStyles.miniLabel);
                GUI.contentColor = prev;
            }

            if (sharedSteps.TryGetValue(stepId, out var names) && names.Count > 0)
            {
                var prev = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.55f, 0.55f);
                EditorGUILayout.LabelField($"  ⚡  Также в: {string.Join(", ", names)}", EditorStyles.miniLabel);
                GUI.contentColor = prev;
            }
        }
    }
}
