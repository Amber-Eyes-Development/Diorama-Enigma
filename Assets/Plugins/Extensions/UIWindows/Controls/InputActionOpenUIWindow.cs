using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Слушатель ввода для открытия окна интерфейса
    /// </summary>
    public class InputActionOpenUIWindow : UIWindowControlInputAction
    {
        [Header("Параметры открытия"), Space]
        [SerializeField] protected UIWindowID UIWindowToOpen;
        [SerializeField] protected bool needToCloseOpened = false;
        [SerializeField] protected bool needToCloseThis = true;
        [Tooltip("Режим открытия: Forward — обычный переход вперёд, Pop — возврат к окну через обрезку хвоста истории")]
        [SerializeField] protected UIWindowOpenMode openMode = UIWindowOpenMode.Forward;

        protected override void OnInputPerformed(InputAction.CallbackContext context) => windowsController.OpenWindow(
            UIWindowToOpen.Id, parentUIWindow, needToCloseThis, needToCloseOpened, openMode);
    }
}