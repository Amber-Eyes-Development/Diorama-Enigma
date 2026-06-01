using Extensions.Generics;
using Extensions.Log;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.Input
{
    /// <summary>
    /// Кнопка запуска изменения привязки действия ввода
    /// </summary>
    public sealed class RebindInputActionButton : AbstractButtonAction
    {
        [SerializeField] private InputActionReference inputActionReference;
        [Min(0)]
        [SerializeField] private int bindingIndex = 0;

        public override void OnButtonClickAction()
        {
            if (BindingController.Instance == null)
            {
                ServiceDebug.LogError($"{nameof(BindingController)} не найден");
                return;
            }

            BindingController.Instance.StartInteractiveRebind(inputActionReference, bindingIndex);
        }
    }
}