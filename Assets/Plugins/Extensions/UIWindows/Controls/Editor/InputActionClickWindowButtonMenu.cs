#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;

namespace Extensions.UIWindows.Editor
{
    /// <summary>
    /// Контекстное добавление хоткея <see cref="InputActionClickWindowButton"/> из меню компонента <see cref="Button"/>
    /// </summary>
    internal static class InputActionClickWindowButtonMenu
    {
        private const string MENU_PATH = "CONTEXT/Button/Add Window Hotkey";

        [MenuItem(MENU_PATH)]
        private static void Add(MenuCommand command)
        {
            Button button = command.context as Button;
            if (button == null) return;

            Undo.AddComponent<InputActionClickWindowButton>(button.gameObject);
        }

        [MenuItem(MENU_PATH, true)]
        private static bool Validate(MenuCommand command)
        {
            return command.context is Button button &&
                   button.GetComponent<InputActionClickWindowButton>() == null;
        }
    }
}
#endif
