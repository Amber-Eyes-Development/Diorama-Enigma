using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Слушатель ввода для закрытия окна интерфейса
    /// </summary>
    public class InputActionCloseUIWindow : UIWindowControlInputAction
    {
        [Header("Параметры закрытия"), Space]
        [SerializeField] protected bool needToOpenPrevious = true;

        protected override void OnInputPerformed(InputAction.CallbackContext context) => windowsController.CloseWindow(
            parentUIWindow, needToOpenPrevious);
    }
}