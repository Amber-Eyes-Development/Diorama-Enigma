using Extensions.Generics;
using Extensions.ScriptableValues;
using UnityEngine;

namespace Extensions.UI
{
    /// <summary>
    /// Отображение значения <see cref="FloatValue"/>
    /// </summary>
    public sealed class FloatValueView : AbstractText
    {
        [Tooltip("Отображаемое значение")]
        [SerializeField] private FloatValue value;
        [Tooltip("Показывать в процентах")]
        [SerializeField] private bool showAsPercent;

        private void OnEnable()
        {
            if (value != null)
            {
                value.onValueChanged += OnValueChanged;
                SetText(value.Value);
            }
        }

        private void OnDisable()
        {
            if (value != null) value.onValueChanged -= OnValueChanged;
        }

        private void OnValueChanged(float value) => SetText(value);

        private void SetText(float value)
        {
            if (showAsPercent)
            {
                UpdateText(Mathf.RoundToInt(value * 100f) + "%");
                return;
            }

            UpdateText(value.ToString("0.##"));
        }
    }
}