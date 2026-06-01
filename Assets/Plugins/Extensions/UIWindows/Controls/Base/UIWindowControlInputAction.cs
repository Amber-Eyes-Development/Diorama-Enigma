using Extensions.Generics.Input;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Абстракция ввода управления окнами <see cref="UIWindow"/>
    /// </summary>
    public abstract class UIWindowControlInputAction : AbstractInputAction
    {
        protected UIWindow parentUIWindow;
        protected UIWindowsController windowsController;
        
        protected virtual void Awake()
        {
            parentUIWindow = GetComponentInParent<UIWindow>();
            windowsController = UIWindowsController.Instance;
        }

        protected override void OnEnable()
        {
            if (windowsController != null)
                windowsController.onLastOpenedWindowChanged += OnLastOpenedWindowChanged;

            UpdateInputListening();
        }

        protected override void OnDisable()
        {
            if (windowsController != null)
                windowsController.onLastOpenedWindowChanged -= OnLastOpenedWindowChanged;

            SetInputListening(false);
        }

        /// <summary>
        /// Обработка изменения последнего открытого окна
        /// </summary>
        protected virtual void OnLastOpenedWindowChanged(UIWindow window) => UpdateInputListening();

        /// <summary>
        /// Обновить состояние слушания ввода
        /// </summary>
        protected virtual void UpdateInputListening()
        {
            if (windowsController == null)
            {
                SetInputListening(false);
                return;
            }

            SetInputListening(windowsController.LastOpenedWindow == parentUIWindow);
        }
    }
}