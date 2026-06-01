using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Слушатель ввода: если есть предыдущее окно — закрыть, иначе открыть указанное окно
    /// </summary>
    public class InputActionCloseOrOpenUIWindow : InputActionOpenUIWindow
    {
        [Header("Параметры закрытия"), Space]
        [SerializeField] protected bool needToOpenPrevious = true;

        protected override void OnInputPerformed(InputAction.CallbackContext context)
        {
            if (windowsController.HasPreviousWindow(parentUIWindow))
            {
                windowsController.CloseWindow(parentUIWindow, needToOpenPrevious);
                return;
            }

            base.OnInputPerformed(context);
        }
    }
}