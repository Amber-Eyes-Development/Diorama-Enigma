using Extensions.Generics;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Связка слайдера с FloatValue
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatValueSliderBinder : AbstractSlider
    {
        [Tooltip("Хранилище значения, синхронизируемое со слайдером")]
        [SerializeField] protected FloatValue floatValue;
        [SerializeField] private bool updateInputOnEnable = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (floatValue == null) return;

            if (updateInputOnEnable) slider.SetValueWithoutNotify(floatValue.Value);
            floatValue.onValueChanged += OnValueChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (floatValue == null) return;

            floatValue.onValueChanged -= OnValueChanged;
        }

        public override void OnSliderValueUpdated(float value) => ApplyValue(value);

        protected virtual void ApplyValue(float value)
        {
            if (floatValue == null) return;

            floatValue.SetValue(value);
        }

        protected virtual void OnValueChanged(float value) => slider.SetValueWithoutNotify(value);
    }
}