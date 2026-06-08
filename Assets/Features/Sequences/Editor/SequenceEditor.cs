using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Инспектор <see cref="Sequence"/>: редактирование записей шагов (ссылки на ассеты + эффекты)
    /// </summary>
    [CustomEditor(typeof(Sequence))]
    public sealed class SequenceEditor : UnityEditor.Editor
    {
        private const string STEPS_FOLDER_PATH = "Assets/Features/Sequences/Data";
        private const string STEPS_FOLDER_NAME = "Steps";

        private const string STEPS_PROP = "steps";
        private const string STEP_PROP = "step";
        private const string GROUP_INDEX_PROP = "groupIndex";
        private const string AVAILABILITY_PROP = "availability";
        private const string ACTIVATION_EFFECTS_PROP = "activationEffects";
        private const string COMPLETION_EFFECTS_PROP = "completionEffects";

        private SerializedProperty sequenceLabelProp;
        private SerializedProperty stepsProp;

        private GUIStyle headerStyle;
        private GUIStyle stepBlockStyle;
        private Texture2D stepBlockBackground;

        private readonly HashSet<int> disconnectedSteps = new();
        private readonly Dictionary<int, List<string>> sharedSteps = new();
        private bool warningsBuilt;

        private readonly Dictionary<int, SerializedObject> stepSOCache = new();

        private static readonly Color SeparatorColor = new(0.45f, 0.65f, 0.95f, 1f);

        private static readonly (string label, Type type)[] StepTypes =
        {
            ("Step", typeof(SequenceStep)),
            ("Composite Step", typeof(CompositeSequenceStep)),
        };

        private static readonly (string label, Type type)[] EffectTypes =
        {
            ("Award Resource", typeof(AwardResourceEffect)),
        };

        private static readonly (string label, Type type)[] GateTypes =
        {
            ("Требует завершения шагов", typeof(RequireStepsCompletedGate)),
        };

        private void OnEnable()
        {
            sequenceLabelProp = serializedObject.FindProperty("sequenceLabel");
            stepsProp = serializedObject.FindProperty(STEPS_PROP);
            RefreshWarnings();
        }

        private void OnDisable()
        {
            stepSOCache.Clear();
        }

        private GUIStyle StepBlockStyle
        {
            get
            {
                if (stepBlockStyle != null)
                {
                    return stepBlockStyle;
                }

                stepBlockStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 8, 8),
                };

                return stepBlockStyle;
            }
        }
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

        #region Groups

        private void DrawGroup(int groupIndex, int position, List<int> groups)
        {
            var rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, SeparatorColor);
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

            var availability = GetGroupAvailability(groupIndex);
            EditorGUI.BeginChangeCheck();
            var nextAvailability = (GroupAvailability)EditorGUILayout.EnumPopup("Доступность", availability);
            if (EditorGUI.EndChangeCheck())
                SetGroupAvailability(groupIndex, nextAvailability);
            EditorGUILayout.Space(2);

            bool firstInGroup = true;
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var entry = stepsProp.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative(GROUP_INDEX_PROP).intValue != groupIndex) continue;

                if (!firstInGroup) EditorGUILayout.Space(6);
                firstInGroup = false;

                DrawStepEntry(entry, i);
            }

            EditorGUILayout.Space(4);
        }

        #endregion

        #region Step entry

        private void DrawStepEntry(SerializedProperty entry, int arrayIndex)
        {
            var stepProp = entry.FindPropertyRelative(STEP_PROP);
            bool stepIsNull = stepProp.objectReferenceValue == null;

            Rect blockRect = EditorGUILayout.BeginVertical(StepBlockStyle);

            if (Event.current.type == EventType.Repaint)
            {
                Rect fillRect = new Rect(
                    blockRect.x + 2f,
                    blockRect.y + 2f,
                    blockRect.width - 4f,
                    blockRect.height - 4f
                );
                
                Color accentColor = EditorToolsConstraints.COLOR_ACCENT;
                accentColor.a = 0.35f;
                EditorGUI.DrawRect(fillRect, accentColor);
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(stepProp, GUIContent.none);

            if (stepIsNull)
            {
                var capturedProp = stepProp.Copy();

                if (GUILayout.Button("Создать ▾", GUILayout.Width(72)))
                {
                    ShowCreateMenuForEntry(capturedProp);
                }
            }

            int moveDir = 0;

            if (GUILayout.Button("▲", GUILayout.Width(20)))
            {
                moveDir = -1;
            }

            if (GUILayout.Button("▼", GUILayout.Width(20)))
            {
                moveDir = +1;
            }

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

            if (stepProp.objectReferenceValue is ScriptableObject stepAsset)
            {
                if (stepAsset is AbstractSequenceStep sequenceStep)
                {
                    string label = string.IsNullOrEmpty(sequenceStep.StepLabel)
                        ? "(без метки)"
                        : sequenceStep.StepLabel;

                    string typeName = stepAsset.GetType().Name.Replace("Sequence", "");
                    EditorGUILayout.LabelField($"  {typeName} · {label}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox($"{stepAsset.GetType().Name} не является шагом ({nameof(AbstractSequenceStep)})", MessageType.Warning);
                }

                DrawStepWarnings(stepAsset.GetInstanceID());
                DrawStepGate(stepAsset);
            }

            DrawManagedRefArray(entry.FindPropertyRelative(ACTIVATION_EFFECTS_PROP), "ЭФФЕКТЫ ПРИ АКТИВАЦИИ", EffectTypes);
            DrawManagedRefArray(entry.FindPropertyRelative(COMPLETION_EFFECTS_PROP), "ЭФФЕКТЫ ПРИ ЗАВЕРШЕНИИ", EffectTypes);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Gate (nested SO editor)

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

        #endregion

        #region SerializeReference array

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

        #endregion

        #region Step creation menus

        /// <summary> Меню создания шага с добавлением новой записи в последовательность </summary>
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
                    var inherited = GetGroupAvailability(capturedGroup);
                    int idx = stepsProp.arraySize;
                    stepsProp.arraySize++;
                    var entry = stepsProp.GetArrayElementAtIndex(idx);
                    entry.FindPropertyRelative(STEP_PROP).objectReferenceValue = asset;
                    entry.FindPropertyRelative(GROUP_INDEX_PROP).intValue = capturedGroup;
                    entry.FindPropertyRelative(AVAILABILITY_PROP).enumValueIndex = (int)inherited;
                    entry.FindPropertyRelative(ACTIVATION_EFFECTS_PROP).ClearArray();
                    entry.FindPropertyRelative(COMPLETION_EFFECTS_PROP).ClearArray();
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Пустой слот (выбрать позже)"), false, () => AddStep(groupIndex));

            menu.ShowAsContext();
        }

        /// <summary> Меню создания шага и назначения его в существующий пустой слот </summary>
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

        #endregion

        #region Asset creation

        private ScriptableObject CreateStepAsset(Type stepType)
        {
            string folder = GetOrCreateStepFolder();
            if (folder == null) return null;

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{stepType.Name}.asset");
            var asset = ScriptableObject.CreateInstance(stepType);

            if (asset is SequenceStep)
            {
                var so = new SerializedObject(asset);
                so.FindProperty("isSaveable").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private string GetOrCreateStepFolder()
        {
            var sequence = (Sequence)target;
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

        #endregion

        #region Effect type menu

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

        #endregion

        #region Step / group management

        private void AddStep(int groupIndex)
        {
            var inherited = GetGroupAvailability(groupIndex);
            int idx = stepsProp.arraySize;
            stepsProp.arraySize++;
            var entry = stepsProp.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative(STEP_PROP).objectReferenceValue = null;
            entry.FindPropertyRelative(GROUP_INDEX_PROP).intValue = groupIndex;
            entry.FindPropertyRelative(AVAILABILITY_PROP).enumValueIndex = (int)inherited;
            entry.FindPropertyRelative(ACTIVATION_EFFECTS_PROP).ClearArray();
            entry.FindPropertyRelative(COMPLETION_EFFECTS_PROP).ClearArray();
        }

        private List<int> GetSortedGroups()
        {
            var set = new SortedSet<int>();
            for (int i = 0; i < stepsProp.arraySize; i++)
                set.Add(stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative(GROUP_INDEX_PROP).intValue);
            return new List<int>(set);
        }

        private GroupAvailability GetGroupAvailability(int groupIndex)
        {
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var entry = stepsProp.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative(GROUP_INDEX_PROP).intValue == groupIndex)
                    return (GroupAvailability)entry.FindPropertyRelative(AVAILABILITY_PROP).enumValueIndex;
            }
            return GroupAvailability.AfterPreviousGroups;
        }

        private void SetGroupAvailability(int groupIndex, GroupAvailability value)
        {
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var entry = stepsProp.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative(GROUP_INDEX_PROP).intValue == groupIndex)
                    entry.FindPropertyRelative(AVAILABILITY_PROP).enumValueIndex = (int)value;
            }
        }

        private void SwapGroups(int groupA, int groupB)
        {
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var gp = stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative(GROUP_INDEX_PROP);
                if (gp.intValue == groupA) gp.intValue = groupB;
                else if (gp.intValue == groupB) gp.intValue = groupA;
            }
        }

        private void MoveStepToAdjacentGroup(int arrayIndex, int direction)
        {
            var groups = GetSortedGroups();
            var gp = stepsProp.GetArrayElementAtIndex(arrayIndex).FindPropertyRelative(GROUP_INDEX_PROP);
            int currentPos = groups.IndexOf(gp.intValue);
            int targetPos = currentPos + direction;

            if (targetPos < 0) gp.intValue = groups[0] - 1;
            else if (targetPos >= groups.Count) gp.intValue = groups[^1] + 1;
            else gp.intValue = groups[targetPos];

            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                if (i == arrayIndex) continue;
                var other = stepsProp.GetArrayElementAtIndex(i);
                if (other.FindPropertyRelative(GROUP_INDEX_PROP).intValue != gp.intValue) continue;

                stepsProp.GetArrayElementAtIndex(arrayIndex).FindPropertyRelative(AVAILABILITY_PROP).enumValueIndex =
                    other.FindPropertyRelative(AVAILABILITY_PROP).enumValueIndex;
                break;
            }
        }

        private int CountStepsInGroup(int groupIndex)
        {
            int count = 0;
            for (int i = 0; i < stepsProp.arraySize; i++)
                if (stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative(GROUP_INDEX_PROP).intValue == groupIndex)
                    count++;
            return count;
        }

        #endregion

        #region Warnings

        private void RefreshWarnings()
        {
            disconnectedSteps.Clear();
            sharedSteps.Clear();
            warningsBuilt = false;

            var valueStepIDs = new HashSet<int>();
            var allStepIDs = new HashSet<int>();

            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var asset = stepsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative(STEP_PROP).objectReferenceValue as ScriptableObject;
                if (asset == null) continue;

                allStepIDs.Add(asset.GetInstanceID());

                if (asset is SequenceStep)
                    valueStepIDs.Add(asset.GetInstanceID());
            }

            if (allStepIDs.Count == 0) { warningsBuilt = true; return; }

            var connectedIDs = new HashSet<int>();
            CollectConnectedStateIDs(connectedIDs, FindObjectsByType<ClickInteractable>(FindObjectsSortMode.None));
            CollectConnectedStateIDs(connectedIDs, FindObjectsByType<DraggableInteractable>(FindObjectsSortMode.None));

            foreach (int id in valueStepIDs)
                if (!connectedIDs.Contains(id))
                    disconnectedSteps.Add(id);

            var guids = AssetDatabase.FindAssets($"t:{nameof(SequenceStep)}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var otherSeq = AssetDatabase.LoadAssetAtPath<Sequence>(path);
                if (otherSeq == null || otherSeq == target) continue;

                var otherSO = new SerializedObject(otherSeq);
                var otherSteps = otherSO.FindProperty(STEPS_PROP);

                for (int i = 0; i < otherSteps.arraySize; i++)
                {
                    var otherAsset = otherSteps.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(STEP_PROP).objectReferenceValue as ScriptableObject;
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
                var stateRef = comp.GetComponent<StepReference>()?.Step;
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

        #endregion
    }
}
