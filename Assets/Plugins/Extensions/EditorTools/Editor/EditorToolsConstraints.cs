using UnityEngine;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Постоянные общие значения скриптов домена EditorTools
    /// </summary>
    public static class EditorToolsConstraints
    {
        public const string PERSISTENT_SERVICE_PROFILE_NAME = "developer";
        
        public const int BASE_FONT_SIZE = 12;
        public const int TEXT_PADDING = 6;
        public const int SPACE_BLOCK_SIZE = 6;
        public const int BASE_ELEMENT_HEIGHT = 24;

        // Единый отступ от краёв окна для всех editor-плагинов
        public const int WINDOW_PADDING = 8;

        public static readonly Color COLOR_GREEN = new Color(0.25f, 0.85f, 0.25f);
        public static readonly Color COLOR_LIGHT_GREEN = new Color(0.75f, 1f, 0.75f);

        public static readonly Color COLOR_YELLOW = new Color(1f, 0.9f, 0.3f);
        public static readonly Color COLOR_RED = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color COLOR_LIGHT_RED = new Color(1f, 0.5f, 0.5f);

        public static readonly Color COLOR_CYAN = new Color(0.5f, 1f, 1f);
        public static readonly Color COLOR_PURPLE = new Color(0.87f, 0.32f, 0.87f);

        public static readonly Color COLOR_ACCENT = new Color(0.45f, 0.45f, 0.45f);

        // Стандартный цвет вспомогательного «фонового» текста (пустые состояния, подсказки)
        public static readonly Color COLOR_MUTED = new Color(0.6f, 0.6f, 0.6f);

        // Имена встроенных иконок редактора (для EditorGUIUtility.IconContent)
        public const string ICON_INSPECT = "Search Icon";
        public const string ICON_SAVE = "SaveAs";
        public const string ICON_ADD = "Toolbar Plus";
        public const string ICON_REMOVE = "TreeEditor.Trash";
        public const string ICON_CAMERA = "SceneViewCamera";
        public const string ICON_SCENE = "SceneAsset Icon";
        public const string ICON_GLOBAL = "ToolHandleGlobal";
        public const string ICON_FOLDER = "FolderOpened Icon";
        public const string ICON_MISSING = "console.erroricon";

        // Символы для текстовых кнопок (фолбэк, когда иконка недоступна)
        public const string SYMBOL_PING = "●";
        public const string SYMBOL_ADD = "+";
        public const string SYMBOL_REMOVE = "✕";
    }
}