using Extensions.EditorTools;
using Extensions.Helpers.Enumerations;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Dioramas.Editor
{
    /// <summary>
    /// Инспектор <see cref="DioramaRegistry"/>
    /// </summary>
    [CustomEditor(typeof(DioramaRegistry))]
    internal sealed class DioramaRegistryEditor : UnityEditor.Editor
    {
        private const string BLOCKS_PROP = "blocks";
        private const string BLOCK_PROP = "block";
        private const string DIORAMAS_PROP = "dioramas";
        private const string DEFINITION_PROP = "definition";
        private const string LINK_OPERATOR_PROP = "linkOperator";
        private const string INCOMING_LINKS_PROP = "incomingLinks";

        private static readonly Color SeparatorColor = new(0.45f, 0.65f, 0.95f, 1f);
        private static readonly Color HintColor = new(0.55f, 0.75f, 1f);

        private SerializedProperty blocksProp;
        private GUIStyle headerStyle;
        private GUIStyle entryBlockStyle;

        private void OnEnable() => blocksProp = serializedObject.FindProperty(BLOCKS_PROP);

        private GUIStyle EntryBlockStyle =>
            entryBlockStyle ??= new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(8, 8, 8, 8) };

        public override void OnInspectorGUI()
        {
            headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

            serializedObject.Update();

            EditorGUILayout.LabelField(target.name, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            int globalIndex = 0;
            for (int b = 0; b < blocksProp.arraySize; b++)
                if (!DrawBlock(b, ref globalIndex)) break; // блок удалён/переставлен — прервать проход, перерисуемся

            EditorGUILayout.Space(2);
            DrawSeparator();
            EditorGUILayout.Space(2);

            if (GUILayout.Button("+ Добавить блок"))
                AddBlock();

            serializedObject.ApplyModifiedProperties();
        }

        #region Block

        /// <returns> false, если список блоков был изменён и проход надо прервать </returns>
        private bool DrawBlock(int blockIndex, ref int globalIndex)
        {
            DrawSeparator();
            EditorGUILayout.Space(2);

            var blockEntry = blocksProp.GetArrayElementAtIndex(blockIndex);
            var blockProp = blockEntry.FindPropertyRelative(BLOCK_PROP);
            var dioramasProp = blockEntry.FindPropertyRelative(DIORAMAS_PROP);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"БЛОК {blockIndex}", headerStyle, GUILayout.Width(70));
            EditorGUILayout.PropertyField(blockProp, GUIContent.none);

            bool up = MoveButton("▲", blockIndex > 0);
            bool down = MoveButton("▼", blockIndex < blocksProp.arraySize - 1);
            bool remove = CompactButton(EditorToolsConstraints.SYMBOL_REMOVE, EditorToolsConstraints.COLOR_LIGHT_RED, "Удалить блок");
            EditorGUILayout.EndHorizontal();

            if (up) { blocksProp.MoveArrayElement(blockIndex, blockIndex - 1); return false; }
            if (down) { blocksProp.MoveArrayElement(blockIndex, blockIndex + 1); return false; }
            if (remove) { blocksProp.DeleteArrayElementAtIndex(blockIndex); return false; }

            EditorGUILayout.Space(2);

            for (int d = 0; d < dioramasProp.arraySize; d++)
            {
                bool isFirstOverall = globalIndex == 0;
                if (!DrawEntry(dioramasProp, d, isFirstOverall)) return false;
                globalIndex++;
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ Добавить диораму в блок"))
                AddEntry(dioramasProp);

            EditorGUILayout.Space(4);
            return true;
        }

        #endregion

        #region Entry

        /// <returns> false, если список диорам был изменён и проход надо прервать </returns>
        private bool DrawEntry(SerializedProperty dioramasProp, int index, bool isFirstOverall)
        {
            var entry = dioramasProp.GetArrayElementAtIndex(index);
            var defProp = entry.FindPropertyRelative(DEFINITION_PROP);
            var operatorProp = entry.FindPropertyRelative(LINK_OPERATOR_PROP);
            var linksProp = entry.FindPropertyRelative(INCOMING_LINKS_PROP);

            EditorGUILayout.BeginVertical(EntryBlockStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"№{index}", GUILayout.Width(28));
            EditorGUILayout.PropertyField(defProp, GUIContent.none);

            bool up = MoveButton("▲", index > 0);
            bool down = MoveButton("▼", index < dioramasProp.arraySize - 1);
            bool remove = CompactButton(EditorToolsConstraints.SYMBOL_REMOVE, EditorToolsConstraints.COLOR_LIGHT_RED, "Удалить диораму");
            EditorGUILayout.EndHorizontal();

            if (up) { dioramasProp.MoveArrayElement(index, index - 1); EditorGUILayout.EndVertical(); return false; }
            if (down) { dioramasProp.MoveArrayElement(index, index + 1); EditorGUILayout.EndVertical(); return false; }
            if (remove) { dioramasProp.DeleteArrayElementAtIndex(index); EditorGUILayout.EndVertical(); return false; }

            DrawAccess(operatorProp, linksProp, isFirstOverall);

            EditorGUILayout.EndVertical();
            return true;
        }

        // Условия доступа: оператор (при 2+ связях), список входящих связей, подсказка при пустом списке
        private void DrawAccess(SerializedProperty operatorProp, SerializedProperty linksProp, bool isFirstOverall)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ДОСТУП", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            bool add = CompactButton(EditorToolsConstraints.SYMBOL_ADD, EditorToolsConstraints.COLOR_LIGHT_GREEN, "Добавить связь");
            EditorGUILayout.EndHorizontal();

            if (linksProp.arraySize == 0)
            {
                Hint(isFirstOverall
                    ? "Стартовая диорама реестра — открыта с самого начала"
                    : "Без связей — откроется, когда решена предыдущая диорама реестра");
            }
            else
            {
                if (linksProp.arraySize >= 2)
                    EditorGUILayout.PropertyField(operatorProp, new GUIContent("Объединять связи"));

                int removeIndex = -1;
                for (int i = 0; i < linksProp.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(linksProp.GetArrayElementAtIndex(i), new GUIContent($"Связь {i}"), true);
                    if (CompactButton(EditorToolsConstraints.SYMBOL_REMOVE, EditorToolsConstraints.COLOR_LIGHT_RED, "Удалить связь"))
                        removeIndex = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeIndex >= 0) linksProp.DeleteArrayElementAtIndex(removeIndex);
            }

            if (add) linksProp.InsertArrayElementAtIndex(linksProp.arraySize);
        }

        #endregion

        #region Helpers

        private void AddBlock()
        {
            int idx = blocksProp.arraySize;
            blocksProp.InsertArrayElementAtIndex(idx);

            var added = blocksProp.GetArrayElementAtIndex(idx);
            added.FindPropertyRelative(BLOCK_PROP).objectReferenceValue = null;
            added.FindPropertyRelative(DIORAMAS_PROP).ClearArray();
        }

        // Новая запись: оператор по умолчанию Or (сериализация не применяет C#-инициализатор), связей нет
        private void AddEntry(SerializedProperty dioramasProp)
        {
            int idx = dioramasProp.arraySize;
            dioramasProp.InsertArrayElementAtIndex(idx);

            var added = dioramasProp.GetArrayElementAtIndex(idx);
            added.FindPropertyRelative(DEFINITION_PROP).objectReferenceValue = null;
            added.FindPropertyRelative(LINK_OPERATOR_PROP).enumValueIndex = (int)LogicOperator.Or;
            added.FindPropertyRelative(INCOMING_LINKS_PROP).ClearArray();
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, SeparatorColor);
        }

        private static void Hint(string text)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = HintColor;
            EditorGUILayout.LabelField("  ℹ  " + text, EditorStyles.miniLabel);
            GUI.contentColor = prev;
        }

        private static bool MoveButton(string symbol, bool enabled)
        {
            using (new EditorGUI.DisabledScope(!enabled))
                return GUILayout.Button(symbol, GUILayout.Width(22), GUILayout.Height(18));
        }

        private static bool CompactButton(string symbol, Color background, string tooltip)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = background;
            bool clicked = GUILayout.Button(new GUIContent(symbol, tooltip), GUILayout.Width(22), GUILayout.Height(18));
            GUI.backgroundColor = prev;
            return clicked;
        }

        #endregion
    }
}
