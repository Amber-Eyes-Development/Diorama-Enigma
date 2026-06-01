using UnityEngine.EventSystems;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Связка слайдера с FloatValue с применением значения после отпускания
    /// </summary>
    public class FloatValueSliderApplyOnReleaseBinder : FloatValueSliderBinder, IPointerDownHandler, IPointerUpHandler
    {
        private bool isInteracting;
        private float pendingValue;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (floatValue == null) return;

            pendingValue = floatValue.Value;
        }

        public override void OnSliderValueUpdated(float value)
        {
            pendingValue = value;

            if (!isInteracting) ApplyValue(pendingValue);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isInteracting = true;
            pendingValue = slider.value;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isInteracting = false;
            ApplyValue(pendingValue);
        }

        protected override void OnValueChanged(float value)
        {
            if (isInteracting) return;

            base.OnValueChanged(value);
            pendingValue = value;
        }
    }
}