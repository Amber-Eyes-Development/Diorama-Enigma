using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.Generics.Input
{
    /// <summary>
    /// Абстракция слушателя ввода <see cref="InputAction"/> для выполнения действий
    /// </summary>
    // TODO Добавить приоритет выполнения действий через аркестратор действий
    public abstract class AbstractInputAction : MonoBehaviour
    {
        [Header("Действия-триггеры"), Space]
        [SerializeField] protected List<InputActionReference> inputActions = new List<InputActionReference>();

        protected bool isListening;

        protected virtual void OnEnable() => SetInputListening(true);
        protected virtual void OnDisable() => SetInputListening(false);

        /// <summary>
        /// Установить состояние слушания ввода
        /// </summary>
        protected void SetInputListening(bool value)
        {
            if (isListening == value) return;

            isListening = value;

            if (isListening)
                EnableInputListening();
            else
                DisableInputListening();
        }

        /// <summary>
        /// Включить слушание ввода
        /// </summary>
        protected virtual void EnableInputListening()
        {
            foreach (InputActionReference inputActionReference in inputActions)
            {
                if (inputActionReference == null || inputActionReference.action == null) continue;

                inputActionReference.action.performed += OnInputActionPerformed;
                inputActionReference.action.Enable();
            }
        }

        /// <summary>
        /// Отключить слушание ввода
        /// </summary>
        protected virtual void DisableInputListening()
        {
            foreach (InputActionReference inputActionReference in inputActions)
            {
                if (inputActionReference == null || inputActionReference.action == null) continue;

                inputActionReference.action.performed -= OnInputActionPerformed;
                inputActionReference.action.Disable();
            }
        }

        private void OnInputActionPerformed(InputAction.CallbackContext context)
        {
            OnInputPerformed();
            OnInputPerformed(context);
        }

        /// <summary>
        /// Выполняемое действие
        /// </summary>
        protected virtual void OnInputPerformed() { }
        
        /// <summary>
        /// Выполняемое действие
        /// </summary>
        protected virtual void OnInputPerformed(InputAction.CallbackContext context) { }
    }
}