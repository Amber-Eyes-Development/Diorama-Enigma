using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Окно интерфейса
    /// </summary>
    public sealed class UIWindow : MonoBehaviour
    {
        /// <summary>
        /// Идентификатор окна
        /// </summary>
        public UIWindowID Id => id;
        /// <summary>
        /// Идентификатор предыдущего окна
        /// </summary>
        public UIWindowID PreviousWindow => previousWindow;
        /// <summary>
        /// Тип окна
        /// </summary>
        public UIWindowType Type => type;

        [SerializeField] private UIWindowID id;
        [SerializeField] private UIWindowType type = UIWindowType.window;

        private UIWindowID previousWindow;

        /// <summary>
        /// Установить предыдущее окно
        /// </summary>
        public void SetPreviousWindow(UIWindowID window) => previousWindow = window;
    }
}