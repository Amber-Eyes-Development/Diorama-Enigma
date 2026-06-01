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

        public static readonly Color COLOR_GREEN = new Color(0.25f, 0.85f, 0.25f);
        public static readonly Color COLOR_LIGHT_GREEN = new Color(0.75f, 1f, 0.75f);
        
        public static readonly Color COLOR_YELLOW = new Color(1f, 0.9f, 0.3f);
        public static readonly Color COLOR_RED = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color COLOR_LIGHT_RED = new Color(1f, 0.5f, 0.5f);

        public static readonly Color COLOR_CYAN = new Color(0.5f, 1f, 1f);
        public static readonly Color COLOR_PURPLE = new Color(0.87f, 0.32f, 0.87f);
    }
}