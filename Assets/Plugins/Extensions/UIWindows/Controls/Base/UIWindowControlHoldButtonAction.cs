using Extensions.Generics;
using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Абстракция зажимоаемой кнопки управления окнами <see cref="UIWindow"/>
    /// </summary>
    public abstract class UIWindowControlHoldButtonAction : AbstractHoldButtonAction
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