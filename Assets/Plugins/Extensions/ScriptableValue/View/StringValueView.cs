using Extensions.Generics;
using Extensions.Helpers;
using Extensions.ScriptableValues;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Отображение значения <see cref="StringValue"/>
    /// </summary>
    public sealed class StringValueView : AbstractText
    {
        [Tooltip("Отображаемое значение")]
        [SerializeField] private StringValue value;
        [Tooltip("Форматировать строку к формату «Hello»")]
        [SerializeField] private bool formatToNormal;

        private void OnEnable()
        {
            if (value != null)
            {
                value.onValueChanged += OnValueChanged;
                UpdateText(value.Value);
            }
        }

        private void OnDisable()
        {
            if (value != null) value.onValueChanged -= OnValueChanged;
        }

        private void OnValueChanged(string value) => UpdateText(value);

        protected override void UpdateText(string value)
        {
            if (formatToNormal)
            {
                base.UpdateText(Formatters.FormatString(value));
                return;
            }

            base.UpdateText(value);
        }
    }
}