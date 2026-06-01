using Extensions.Generics;
using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Абстракция кнопки управления окнами <see cref="UIWindow"/>
    /// </summary>
    public abstract class UIWindowControlButtonAction : AbstractButtonAction
    {
        protected UIWindow parentUIWindow;
        protected UIWindowsController windowsController;
        
        protected override void Awake()
        {
            base.Awake();
            parentUIWindow = GetComponentInParent<UIWindow>();
            windowsController = UIWindowsController.Instance;
        }
    }
}