using Extensions.Generics;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Связка слайдера с <see cref="IntValue"/>
    /// </summary>
    [DisallowMultipleComponent]
    public class IntValueSliderBinder : AbstractSlider
    {
        [Tooltip("Хранилище значения, синхронизируемое со слайдером")]
        [SerializeField] protected IntValue intValue;
        [SerializeField] private bool updateInputOnEnable = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (intValue == null) return;

            if (updateInputOnEnable) slider.SetValueWithoutNotify(intValue.Value);
            intValue.onValueChanged += OnValueChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (intValue == null) return;

            intValue.onValueChanged -= OnValueChanged;
        }

        public override void OnSliderValueUpdated(float value) => ApplyValue(Mathf.RoundToInt(value));

        protected virtual void ApplyValue(int value)
        {
            if (intValue == null) return;

            intValue.SetValue(value);
        }

        protected virtual void OnValueChanged(int value) => slider.SetValueWithoutNotify(value);
    }
}