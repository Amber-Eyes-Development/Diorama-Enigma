#if RIDDLES_EDITOR_WIP
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Инспектор PuzzleSequence с inline-редактированием шагов и групп
    /// </summary>
    [CustomEditor(typeof(PuzzleSequence))]
    public sealed class PuzzleSequenceEditor : UnityEditor.Editor
    {
        private SerializedProperty sequenceLabelProp;
        private SerializedProperty stepsProp;

        private GUIStyle headerStyle;

        private static readonly Color SeparatorEven = new(0.25f, 0.45f, 0.65f, 1f);
        private static readonly Color SeparatorOdd = new(0.35f, 0.55f, 0.35f, 1f);

        // Типы для дропдаунов добавления
        private static readonly (string label, Type type)[] ConditionTypes =
        {
            ("Click", typeof(ClickCondition)),
            ("Drag", typeof(DragCondition)),
            ("Resource", typeof(ResourceCondition)),
            ("Delayed (обёртка)", typeof(DelayedCondition)),
        };

        // Для вложенного условия (DelayedCondition.inner) — без Delayed, чтобы избежать бесконечной вложенности
        private static readonly (string label, Type type)[] InnerConditionTypes =
        {
            ("Click", typeof(ClickCondition)),
            ("Drag", typeof(DragCondition)),
            ("Resource", typeof(ResourceCondition)),
        };

        private static readonly (string label, Type type)[] EffectTypes =
        {
            ("Award Resource", typeof(AwardResourceEffect)),
            ("Fire Event", typeof(FireEventEffect)),
            ("Set Lock", typeof(SetInteractableLockEffect)),
        };

        private void OnEnable()
        {
            sequenceLabelProp = serializedObject.FindProperty("sequenceLabel");
            stepsProp = serializedObject.FindProperty("steps");
        }

        public override void OnInspectorGUI()
        {
            headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

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

        // ─── Группы ───────────────────────────────────────────────────────────

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

        // ─── Шаг ──────────────────────────────────────────────────────────────

        private void DrawStepEntry(SerializedProperty entry, int arrayIndex)
        {
            var stepProp = entry.FindPropertyRelative("Step");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool stepIsNull = string.IsNullOrEmpty(stepProp.managedReferenceFullTypename);

            if (stepIsNull)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("(пустой шаг)");
                if (GUILayout.Button("Создать", GUILayout.Width(70)))
                    stepProp.managedReferenceValue = new PuzzleStep();
                if (GUILayout.Button("✕", GUILayout.Width(20)))
                    stepsProp.DeleteArrayElementAtIndex(arrayIndex);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            var labelProp = stepProp.FindPropertyRelative("stepLabel");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(labelProp, GUIContent.none);

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

            DrawManagedRefArray(stepProp.FindPropertyRelative("conditions"), "УСЛОВИЯ (все должны выполниться)", ConditionTypes);
            DrawManagedRefArray(stepProp.FindPropertyRelative("activationEffects"), "ЭФФЕКТЫ ПРИ АКТИВАЦИИ", EffectTypes);
            DrawManagedRefArray(stepProp.FindPropertyRelative("effects"), "ЭФФЕКТЫ ПРИ ЗАВЕРШЕНИИ", EffectTypes);
            DrawManagedRefArray(stepProp.FindPropertyRelative("failureEffects"), "ЭФФЕКТЫ ПРИ ПРОВАЛЕ", EffectTypes);

            EditorGUILayout.EndVertical();
        }

        // ─── SerializeReference массив ────────────────────────────────────────

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

        /// <summary>Рисует редактируемые поля managed-reference объекта без дефолтного фолдаута.</summary>
        private void DrawManagedRefBody(SerializedProperty prop)
        {
            var end = prop.GetEndProperty();
            var it = prop.Copy();
            bool enter = true;

            EditorGUI.indentLevel++;

            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = false;

                // Вложенное полиморфное поле (DelayedCondition.inner) — рисуем со своим пикером типа
                if (it.propertyType == SerializedPropertyType.ManagedReference)
                    DrawNestedManagedRef(it.Copy());
                else
                    EditorGUILayout.PropertyField(it, true);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawNestedManagedRef(SerializedProperty prop)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            bool isNull = string.IsNullOrEmpty(prop.managedReferenceFullTypename);
            EditorGUILayout.LabelField($"{prop.displayName}: {(isNull ? "(не выбрано)" : ManagedRefTypeName(prop))}",
                EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isNull ? "Выбрать" : "Сменить", GUILayout.Width(70)))
                ShowPickMenu(prop.propertyPath, InnerConditionTypes);
            EditorGUILayout.EndHorizontal();

            if (!isNull) DrawManagedRefBody(prop);

            EditorGUILayout.EndVertical();
        }

        // ─── Меню выбора типа ─────────────────────────────────────────────────

        private void ShowAddMenu(string arrayPath, (string label, Type type)[] choices)
        {
            var menu = new GenericMenu();

            foreach (var (label, type) in choices)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(label), false, () =>
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

        private void ShowPickMenu(string propPath, (string label, Type type)[] choices)
        {
            var menu = new GenericMenu();

            foreach (var (label, type) in choices)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    serializedObject.Update();
                    serializedObject.FindProperty(propPath).managedReferenceValue = Activator.CreateInstance(captured);
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

        // ─── Управление шагами/группами ───────────────────────────────────────

        private void AddStep(int groupIndex)
        {
            int idx = stepsProp.arraySize;
            stepsProp.arraySize++;
            var entry = stepsProp.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("Step").managedReferenceValue = new PuzzleStep();
            entry.FindPropertyRelative("GroupIndex").intValue = groupIndex;
        }

        private List<int> GetSortedGroups()
        {
            var set = new SortedSet<int>();
            for (int i = 0; i < stepsProp.arraySize; i++)
                set.Add(stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("GroupIndex").intValue);

            return new List<int>(set);
        }

        private void SwapGroups(int groupA, int groupB)
        {
            for (int i = 0; i < stepsProp.arraySize; i++)
            {
                var gp = stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("GroupIndex");
                if (gp.intValue == groupA) gp.intValue = groupB;
                else if (gp.intValue == groupB) gp.intValue = groupA;
            }
        }

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
                if (stepsProp.GetArrayElementAtIndex(i).FindPropertyRelative("GroupIndex").intValue == groupIndex)
                    count++;

            return count;
        }
    }
}
#endif
