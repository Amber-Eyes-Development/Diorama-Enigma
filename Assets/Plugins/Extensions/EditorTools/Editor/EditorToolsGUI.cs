using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Общие IMGUI-хелперы компонент UI editor-плагинов
    /// </summary>
    public static class EditorToolsGUI
    {
        /// <summary>
        /// Кнопка с иконкой слева и подписью. Текст рисуется всегда, иконка — поверх, если доступна
        /// </summary>
        public static bool IconButton(string iconName, string text, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent("  " + text);

            List<GUILayoutOption> layout = new List<GUILayoutOption>(options.Length + 1)
            {
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)
            };
            layout.AddRange(options);

            Rect rect = GUILayoutUtility.GetRect(content, GUI.skin.button, layout.ToArray());
            bool clicked = GUI.Button(rect, content);

            Texture icon = EditorGUIUtility.IconContent(iconName).image;
            if (icon != null)
            {
                float iconSize = EditorToolsConstraints.BASE_ELEMENT_HEIGHT - 8;
                Rect iconRect = new Rect(rect.x + 4, rect.y + (rect.height - iconSize) / 2, iconSize, iconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            return clicked;
        }

        /// <summary>
        /// Квадратная кнопка-иконка. При отсутствии иконки показывает текстовый фолбэк-символ
        /// </summary>
        public static bool IconButtonSquare(string iconName, string fallbackSymbol, string tooltip = null)
        {
            return GUILayout.Button(
                IconOrText(iconName, fallbackSymbol, tooltip),
                GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT),
                GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT));
        }

        /// <summary>
        /// Контент «иконка или текст»: иконка с подписью, либо только текст, если иконка недоступна
        /// </summary>
        public static GUIContent IconText(string iconName, string text, string tooltip = null)
        {
            Texture icon = EditorGUIUtility.IconContent(iconName).image;

            return icon != null
                ? new GUIContent(" " + text, icon, tooltip)
                : new GUIContent(text, tooltip);
        }

        /// <summary>
        /// Контент «иконка либо текстовый символ» для компактных квадратных кнопок
        /// </summary>
        public static GUIContent IconOrText(string iconName, string fallbackSymbol, string tooltip = null)
        {
            Texture icon = EditorGUIUtility.IconContent(iconName).image;

            return icon != null
                ? new GUIContent(icon, tooltip)
                : new GUIContent(fallbackSymbol, tooltip);
        }

        /// <summary>
        /// Тонкая горизонтальная линия-разделитель во всю ширину
        /// </summary>
        public static void Separator()
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorToolsConstraints.COLOR_ACCENT);
        }
    }
}
