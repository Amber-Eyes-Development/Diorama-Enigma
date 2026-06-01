using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    [CustomEditor(typeof(PuzzleSequence))]
    public sealed class PuzzleSequenceEditor : UnityEditor.Editor
    {
        private SerializedProperty sequenceLabelProp;
        private SerializedProperty stepsProp;

        private GUIStyle headerStyle;
        private GUIStyle smallStyle;

        private static readonly Color SeparatorColorEven = new(0.25f, 0.45f, 0.65f, 1f);
        private static readonly Color SeparatorColorOdd = new(0.35f, 0.55f, 0.35f, 1f);

        private static readonly Color ColorClick = new(0.4f, 0.7f, 1f);
        private static readonly Color ColorDrag = new(1f, 0.65f, 0.3f);
        private static readonly Color ColorResource = new(0.4f, 1f, 0.5f);

        private void OnEnable()
        {
            sequenceLabelProp = serializedObject.FindProperty("sequenceLabel");
            stepsProp = serializedObject.FindProperty("steps");
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();

            serializedObject.Update();

            EditorGUILayout.PropertyField(sequenceLabelProp);
            EditorGUILayout.Space(4);

            var groups = GetSortedGroups();

            for (int position = 0; position < groups.Count; position++)
                DrawGroup(groups[position], position, groups);

            EditorGUILayout.Space(4);

            int maxGroup = groups.Count > 0 ? groups[^1] : -1;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Шаг (новая группа)"))
                AddStep(maxGroup + 1);

            if (groups.Count > 0 && GUILayout.Button("+ Шаг (в последнюю группу)"))
                AddStep(maxGroup);

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void EnsureStyles()
        {
            headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            smallStyle ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        }

        private void DrawGroup(int groupIndex, int position, List<int> groups)
        {
            Color separatorColor = position % 2 == 0 ? SeparatorColorEven : SeparatorColorOdd;

            var rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, separatorColor);
            EditorGUILayout.Space(2);

            int count = CountStepsInGroup(groupIndex);
            string groupLabel = count > 1
                ? $"Группа {position}  —  {count} шага параллельно"
                : $"Группа {position}";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(groupLabel, headerStyle);
            GUILayout.FlexibleSpace();

            // Переместить всю группу выше/ниже в очереди выполнения (swap с соседней)
            bool moveUp = false;
            bool moveDown = false;

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
                if (entry.FindPropertyRelative("GroupIndex").intValue != groupIndex) continue;

                DrawStepEntry(entry, i);
            }

            EditorGUILayout.Space(2);
        }

        private void DrawStepEntry(SerializedProperty entry, int arrayIndex)
        {
            var stepProp = entry.FindPropertyRelative("Step");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(stepProp, GUIContent.none);

            // Переместить шаг в предыдущую/следующую группу (в т.ч. вынести в новую крайнюю)
            int moveStepDir = 0;
            if (GUILayout.Button("▲", GUILayout.Width(20))) moveStepDir = -1;
            if (GUILayout.Button("▼", GUILayout.Width(20))) moveStepDir = +1;

            bool delete = GUILayout.Button("✕", GUILayout.Width(20));

            EditorGUILayout.EndHorizontal();

            if (delete)
            {
                stepsProp.DeleteArrayElementAtIndex(arrayIndex);
                EditorGUILayout.EndVertical();
                return;
            }

            if (moveStepDir != 0)
            {
                MoveStepToAdjacentGroup(arrayIndex, moveStepDir);
                EditorGUILayout.EndVertical();
                return;
            }

            if (stepProp.objectReferenceValue is PuzzleStep step)
            {
                DrawConditionsSummary(step);

                if (step.Conditions.Count == 0)
                    EditorGUILayout.HelpBox("Шаг не имеет условий и завершится немедленно.", MessageType.Warning);

                if (step.ActivationEffects.Count > 0)
                {
                    var activationStyle = new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(1f, 0.85f, 0.3f) } };
                    EditorGUILayout.LabelField($"  ▶ on activate: {step.ActivationEffects.Count} effect(s)", activationStyle);
                }

                if (step.FailureEffects.Count > 0)
                {
                    var failStyle = new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
                    EditorGUILayout.LabelField($"  ✕ on failure: {step.FailureEffects.Count} effect(s)", failStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConditionsSummary(PuzzleStep step)
        {
            if (step.Conditions.Count == 0) return;

            EditorGUI.indentLevel++;

            foreach (var condition in step.Conditions)
            {
                if (condition == null)
                {
                    EditorGUILayout.LabelField("  ○ (null condition)", smallStyle);
                    continue;
                }

                string summary = GetConditionSummary(condition);
                string typeName = condition.GetType().Name.Replace("Condition", "");
                Color color = GetConditionColor(condition);

                var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{typeName}]", labelStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField(summary, smallStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        private static string GetConditionSummary(PuzzleCondition condition)
        {
            return condition switch
            {
                DelayedCondition delayed =>
                    $"{(delayed.Inner != null ? GetConditionSummary(delayed.Inner) : "?")}  +{delayed.DelaySeconds:0.##}s",
                ClickCondition click =>
                    $"{(click.TargetId != null ? click.TargetId.name : "?")} → state {click.RequiredStateIndex}",
                DragCondition drag =>
                    $"{(drag.DraggableId != null ? drag.DraggableId.name : "?")} → {(drag.DropZoneId != null ? drag.DropZoneId.name : "?")}",
                ResourceCondition res =>
                    $"{(res.Resource != null ? res.Resource.name : "?")} == {res.RequiredValue}",
                _ => condition.GetType().Name
            };
        }

        private static Color GetConditionColor(PuzzleCondition condition)
        {
            return condition switch
            {
                DelayedCondition delayed => delayed.Inner != null
                    ? GetConditionColor(delayed.Inner)
                    : Color.white,
                ClickCondition => ColorClick,
                DragCondition => ColorDrag,
                ResourceCondition => ColorResource,
                _ => Color.white
            };
        }

        private void AddStep(int groupIndex)
        {
            stepsProp.arraySize++;
            var newEntry = stepsProp.GetArrayElementAtIndex(stepsProp.arraySize - 1);
            newEntry.FindPropertyRelative("Step").objectReferenceValue = null;
            newEntry.FindPropertyRelative("GroupIndex").intValue = groupIndex;
        }

        /// <summary>Уникальные значения GroupIndex по возрастанию (порядок выполнения групп)</summary>
        private List<int> GetSortedGroups()
        {
            var set = new SortedSet<int>();
            for (int i = 0; i < stepsProp.arraySize; i++)
                set.Add(stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("GroupIndex").intValue);

            return new List<int>(set);
        }

        /// <summary>Поменять местами две группы — у всех их шагов обменять GroupIndex</summary>
        private void SwapGroups(int groupA, int groupB)
        {
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var gp = stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("GroupIndex");
                if (gp.intValue == groupA) gp.intValue = groupB;
                else if (gp.intValue == groupB) gp.intValue = groupA;
            }
        }

        /// <summary>
        /// Переместить шаг в соседнюю группу. Если соседней нет (крайняя позиция) —
        /// вынести шаг в новую крайнюю группу.
        /// </summary>
        private void MoveStepToAdjacentGroup(int arrayIndex, int direction)
        {
            var groups = GetSortedGroups();
            var gp = stepsProp.GetArrayElementAtIndex(arrayIndex).FindPropertyRelative("GroupIndex");

            int currentPos = groups.IndexOf(gp.intValue);
            int targetPos = currentPos + direction;

            if (targetPos < 0)
                gp.intValue = groups[0] - 1;
            else if (targetPos >= groups.Count)
                gp.intValue = groups[^1] + 1;
            else
                gp.intValue = groups[targetPos];
        }

        private int CountStepsInGroup(int groupIndex)
        {
            int count = 0;
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                if (stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("GroupIndex").intValue == groupIndex)
                    count++;
            }

            return count;
        }
    }
}
