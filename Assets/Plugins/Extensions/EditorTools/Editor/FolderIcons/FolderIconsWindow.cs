using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools.FolderIcons
{
    /// <summary>
    /// Окно управления кастомными иконками папок и выделением папок Features
    /// </summary>
    public sealed class FolderIconsWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Folder Icons";
        private const string RULES_PROPERTY = "rules";
        private const string BOLD_PROPERTY = "boldFeaturesChildren";
        private const string RULE_NAME_FIELD = nameof(FolderIconRule.FolderName);
        private const string RULE_ICON_FIELD = nameof(FolderIconRule.IconName);

        private const float ICON_BUTTON_SIZE = 36f;
        private const float FIELD_LABEL_WIDTH = 60f;
        private const float PICK_BUTTON_WIDTH = 56f;

        [SerializeField]
        private FolderIconsSettings settings;

        private SerializedObject serialized;
        private SerializedProperty rulesProperty;
        private SerializedProperty boldProperty;
        private Vector2 scroll;

        [MenuItem("Tools/" + WINDOW_NAME)]
        public static void Open()
        {
            FolderIconsWindow window = GetWindow<FolderIconsWindow>();
            window.titleContent = new GUIContent(WINDOW_NAME);
            window.Show();
        }

        private void OnEnable()
        {
            settings = FolderIconsSettings.GetOrCreate();

            serialized = new SerializedObject(settings);
            rulesProperty = serialized.FindProperty(RULES_PROPERTY);
            boldProperty = serialized.FindProperty(BOLD_PROPERTY);
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("FolderIconsSettings is not available", MessageType.Error);
                return;
            }

            serialized.Update();

            using (new EditorGUILayout.VerticalScope(EditorToolsStyles.WindowPadding))
            {
                DrawBoldToggle();

                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorToolsGUI.Separator();
                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

                GUILayout.Label("Папки с указанным именем получают встроенную иконку в окне Project", EditorToolsStyles.MutedLabel);

                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                DrawAddButton();
                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

                scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                DrawRules();
                GUILayout.EndScrollView();
            }

            if (serialized.ApplyModifiedProperties())
                FolderIconsDrawer.RefreshAndRepaint();
        }

        private void DrawBoldToggle()
        {
            EditorGUILayout.PropertyField(boldProperty, new GUIContent("Bold Features children", "Выделять жирным имена всех папок внутри папки Features"));
        }

        private void DrawAddButton()
        {
            if (EditorToolsGUI.IconButton(EditorToolsConstraints.ICON_ADD, "Add Rule", GUILayout.ExpandWidth(true)))
                AddRule();
        }

        private void DrawRules()
        {
            if (rulesProperty.arraySize == 0)
            {
                GUILayout.Label("No rules yet", EditorToolsStyles.MutedLabel);
                return;
            }

            for (int i = 0; i < rulesProperty.arraySize; i++)
            {
                if (DrawRuleRow(i))
                {
                    rulesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }

        /// <summary>Рисует одну строку правила, возвращает true если запрошено удаление</summary>
        private bool DrawRuleRow(int index)
        {
            SerializedProperty rule = rulesProperty.GetArrayElementAtIndex(index);
            SerializedProperty nameProperty = rule.FindPropertyRelative(RULE_NAME_FIELD);
            SerializedProperty iconProperty = rule.FindPropertyRelative(RULE_ICON_FIELD);

            bool removeRequested = false;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                DrawIconButton(index, iconProperty);

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawLabeledField("Folder", nameProperty);
                    DrawIconNameField(index, iconProperty);
                }

                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_RED))
                {
                    if (EditorToolsGUI.IconButtonSquare(EditorToolsConstraints.ICON_REMOVE, EditorToolsConstraints.SYMBOL_REMOVE, "Remove"))
                        removeRequested = true;
                }
            }

            return removeRequested;
        }

        private void DrawIconButton(int index, SerializedProperty iconProperty)
        {
            GUIContent preview = string.IsNullOrEmpty(iconProperty.stringValue)
                ? new GUIContent(EditorGUIUtility.IconContent(EditorToolsConstraints.ICON_ADD)) { tooltip = "Выбрать иконку" }
                : new GUIContent(EditorGUIUtility.IconContent(iconProperty.stringValue)) { tooltip = iconProperty.stringValue };

            if (GUILayout.Button(preview, GUILayout.Width(ICON_BUTTON_SIZE), GUILayout.Height(ICON_BUTTON_SIZE)))
                OpenPicker(index);
        }

        private void DrawLabeledField(string label, SerializedProperty property)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(FIELD_LABEL_WIDTH));
                property.stringValue = EditorGUILayout.TextField(property.stringValue);
            }
        }

        private void DrawIconNameField(int index, SerializedProperty iconProperty)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Icon", GUILayout.Width(FIELD_LABEL_WIDTH));

                iconProperty.stringValue = EditorGUILayout.TextField(iconProperty.stringValue);

                if (GUILayout.Button("Pick", GUILayout.Width(PICK_BUTTON_WIDTH)))
                    OpenPicker(index);
            }
        }

        private void OpenPicker(int index)
        {
            FolderIconPickerWindow.Open(iconName =>
            {
                serialized.Update();

                SerializedProperty rule = rulesProperty.GetArrayElementAtIndex(index);
                rule.FindPropertyRelative(RULE_ICON_FIELD).stringValue = iconName;

                serialized.ApplyModifiedProperties();
                FolderIconsDrawer.RefreshAndRepaint();
                Repaint();
            });
        }

        private void AddRule()
        {
            int index = rulesProperty.arraySize;
            rulesProperty.InsertArrayElementAtIndex(index);

            SerializedProperty rule = rulesProperty.GetArrayElementAtIndex(index);
            rule.FindPropertyRelative(RULE_NAME_FIELD).stringValue = string.Empty;
            rule.FindPropertyRelative(RULE_ICON_FIELD).stringValue = string.Empty;
        }
    }
}
