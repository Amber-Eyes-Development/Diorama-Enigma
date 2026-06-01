using Extensions.Generics;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Связка птоггла с <see cref="boolValue"/>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoolValueToggleBinder : AbstractToggle
    {
        [Tooltip("Хранилище значения, синхронизируемое с тогглом")]
        [SerializeField] private BoolValue boolValue;
        [SerializeField] private bool updateInputOnEnable = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (boolValue == null) return;

            if (updateInputOnEnable) toggle.SetIsOnWithoutNotify(boolValue.Value);
            boolValue.onValueChanged += OnValueChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (boolValue == null) return;

            boolValue.onValueChanged -= OnValueChanged;
        }

        public override void OnToggled(bool state) => ApplyValue(state);

        private void ApplyValue(bool state)
        {
            if (boolValue == null) return;

            boolValue.SetValue(state);
        }

        private void OnValueChanged(bool state) => toggle.SetIsOnWithoutNotify(state);
    }
}