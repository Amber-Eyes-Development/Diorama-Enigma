using Extensions.Generics;
using TMPro;
using UnityEngine;

namespace Extensions.UI 
{
    /// <summary>
    /// Блокировка горизонтальной прокрутки текста, пока <see cref="TMP_InputField"/> интерактивен
    /// </summary>
    public sealed class HorizontalTextScrollBlocker : AbstractInputField
    {
        [SerializeField] private HorizontalTextScroller textScroller;

        protected override void Awake()
        {
            base.Awake();
            
            if ((inputField.textComponent.enableAutoSizing ||
                inputField.lineType != TMP_InputField.LineType.SingleLine) &&
                textScroller != null)
            {
                textScroller.enabled = false;
                enabled = false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            inputField.onSelect.AddListener(OnInputFieldSelected);
            inputField.onDeselect.AddListener(OnInputFieldDeselected);
        }

        protected override  void OnDisable()
        {
            base.OnDisable();
            inputField.onSelect.RemoveListener(OnInputFieldSelected);
            inputField.onDeselect.RemoveListener(OnInputFieldDeselected);

            if (textScroller != null) textScroller.IsBlocked = false;
        }

        private void OnInputFieldSelected(string value)
        {
            textScroller.IsBlocked = true;
            textScroller.ResetScroll();
        }

        private void OnInputFieldDeselected(string value) => textScroller.IsBlocked = false;
    }
}