using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools.FolderIcons
{
    /// <summary>
    /// Всплывающий выбор встроенной иконки редактора: поиск + сетка иконок.
    /// Возвращает выбранное имя через колбэк, переданный в Open
    /// </summary>
    public sealed class FolderIconPickerWindow : EditorWindow
    {
        private const float CELL_SIZE = 34f;
        private const float ICON_SIZE = 20f;

        private Action<string> onPick;
        private string search = string.Empty;
        private Vector2 scroll;

        /// <summary>
        /// Открыть пикер. onPick вызывается с именем выбранной иконки
        /// </summary>
        public static void Open(Action<string> onPick)
        {
            FolderIconPickerWindow window = CreateInstance<FolderIconPickerWindow>();

            window.onPick = onPick;
            window.titleContent = new GUIContent("Pick Icon");
            window.minSize = new Vector2(360f, 320f);

            window.ShowAuxWindow();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorToolsStyles.WindowPadding))
            {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

                IReadOnlyList<string> all = BuiltInEditorIcons.Names;

                if (all.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Список встроенных иконок недоступен. Введите имя иконки вручную в окне Folder Icons.",
                        MessageType.Info);
                    return;
                }

                DrawGrid(all);
            }
        }

        private void DrawGrid(IReadOnlyList<string> all)
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - EditorToolsConstraints.WINDOW_PADDING * 2f) / CELL_SIZE));

            scroll = GUILayout.BeginScrollView(scroll);

            int columnInRow = 0;
            bool rowOpen = false;

            for (int i = 0; i < all.Count; i++)
            {
                string iconName = all[i];

                if (!Matches(iconName)) continue;

                GUIContent content = EditorGUIUtility.IconContent(iconName);
                if (content == null || content.image == null) continue;

                if (columnInRow == 0)
                {
                    GUILayout.BeginHorizontal();
                    rowOpen = true;
                }

                GUIContent cell = new GUIContent(content.image, iconName);

                if (GUILayout.Button(cell, GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE)))
                {
                    onPick?.Invoke(iconName);
                    Close();

                    if (rowOpen) GUILayout.EndHorizontal();
                    GUILayout.EndScrollView();
                    return;
                }

                columnInRow++;

                if (columnInRow >= columns)
                {
                    GUILayout.EndHorizontal();
                    rowOpen = false;
                    columnInRow = 0;
                }
            }

            if (rowOpen) GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        private bool Matches(string iconName)
        {
            return string.IsNullOrEmpty(search) ||
                   iconName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
