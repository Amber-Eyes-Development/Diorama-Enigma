using Extensions.Generics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Extensions.Input
{
    /// <summary>
    /// Текст привязки действия ввода
    /// </summary>
    public sealed class InputActionBindingText : AbstractText
    {
        #region Параметры
        [SerializeField] private InputActionReference inputActionReference;
        [Min(0)]
        [SerializeField] private int bindingIndex = 0;
        #endregion
        
        #region MonoBehaviour
        
        private void OnEnable()
        {
            if (BindingController.Instance != null)
            {
                BindingController.Instance.onRebindStarted += OnRebindStarted;
                BindingController.Instance.onRebindCanceled += OnBindingChanged;
                BindingController.Instance.onBindingChanged += OnBindingChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (BindingController.Instance == null) return;

            BindingController.Instance.onRebindStarted -= OnRebindStarted;
            BindingController.Instance.onRebindCanceled -= OnBindingChanged;
            BindingController.Instance.onBindingChanged -= OnBindingChanged;
        }
        
        #endregion
        
        /// <summary>
        /// Обновить текст привязки
        /// </summary>
        public void Refresh()
        {
            if (BindingController.Instance == null) return;

            UpdateText(BindingController.Instance.GetBindingDisplayString(inputActionReference, bindingIndex));
        }

        private void OnRebindStarted(InputActionReference changedAction, int changedBindingIndex)
        {
            if (!IsTargetBinding(changedAction, changedBindingIndex)) return;

            UpdateText(BindingController.Instance.RebindingWaitingText);
        }

        private void OnBindingChanged(InputActionReference changedAction, int changedBindingIndex)
        {
            if (!IsTargetBinding(changedAction, changedBindingIndex)) return;

            Refresh();
        }

        private bool IsTargetBinding(InputActionReference changedAction, int changedBindingIndex)
        {
            return changedAction == inputActionReference && changedBindingIndex == bindingIndex;
        }
    }
}