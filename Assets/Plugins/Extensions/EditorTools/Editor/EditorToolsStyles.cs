using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Общие GUI-стили editor-плагинов
    /// </summary>
    /// <remarks>
    /// Стили строятся лениво (GUI.skin/EditorStyles доступны только внутри OnGUI) и кэшируются;
    /// статика сбрасывается при перекомпиляции, поэтому пересоздаётся автоматически
    /// </remarks>
    public static class EditorToolsStyles
    {
        private static GUIStyle _windowPadding;
        private static GUIStyle _header;
        private static GUIStyle _rowButton;
        private static GUIStyle _boldFoldout;
        private static GUIStyle _mutedLabel;

        /// <summary>Корневой контейнер окна с одинаковым отступом от всех краёв</summary>
        public static GUIStyle WindowPadding
        {
            get
            {
                if (_windowPadding == null)
                {
                    _windowPadding = new GUIStyle
                    {
                        padding = new RectOffset(
                            EditorToolsConstraints.WINDOW_PADDING,
                            EditorToolsConstraints.WINDOW_PADDING,
                            EditorToolsConstraints.WINDOW_PADDING,
                            EditorToolsConstraints.WINDOW_PADDING)
                    };
                }

                return _windowPadding;
            }
        }

        /// <summary>Заголовок блока</summary>
        public static GUIStyle Header
        {
            get
            {
                if (_header == null)
                {
                    _header = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                        padding = new RectOffset(0, 0, 4, 4)
                    };
                }

                return _header;
            }
        }

        /// <summary>Кнопка-строка списка (текст слева, фиксированная высота)</summary>
        public static GUIStyle RowButton
        {
            get
            {
                if (_rowButton == null)
                {
                    _rowButton = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                        alignment = TextAnchor.MiddleLeft,
                        fixedHeight = EditorToolsConstraints.BASE_ELEMENT_HEIGHT,
                        padding = new RectOffset(EditorToolsConstraints.TEXT_PADDING, EditorToolsConstraints.TEXT_PADDING, 0, 0)
                    };
                }

                return _rowButton;
            }
        }

        /// <summary>Жирный сворачиваемый заголовок секции</summary>
        public static GUIStyle BoldFoldout
        {
            get
            {
                if (_boldFoldout == null)
                {
                    _boldFoldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = EditorToolsConstraints.BASE_FONT_SIZE
                    };
                }

                return _boldFoldout;
            }
        }

        /// <summary>Приглушённая курсивная подпись для пустых состояний и подсказок</summary>
        public static GUIStyle MutedLabel
        {
            get
            {
                if (_mutedLabel == null)
                {
                    _mutedLabel = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Italic,
                        fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                        normal = { textColor = EditorToolsConstraints.COLOR_MUTED }
                    };
                }

                return _mutedLabel;
            }
        }
    }
}
