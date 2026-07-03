using Extensions.Log;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Хоткей кнопки
    /// </summary>
    /// <remarks>
    /// Активен, когда окно <see cref="UIWindow"/> в фокусе
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class InputActionClickWindowButton : UIWindowControlInputAction
    {
        protected Button targetButton;

        protected override void Awake()
        {
            base.Awake();
            targetButton = GetComponent<Button>();
        }

        protected override void OnInputPerformed(InputAction.CallbackContext context)
        {
            if (targetButton == null)
            {
                ServiceDebug.LogError($"{nameof(targetButton)} не назначена в {nameof(InputActionClickWindowButton)}");
                return;
            }

            if (!targetButton.IsActive() || !targetButton.IsInteractable()) return;
            targetButton.onClick.Invoke();
        }
    }
}
