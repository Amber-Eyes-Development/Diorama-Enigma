using System;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция слайдера
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public abstract class AbstractSlider : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое после изменения значения слайдера
        /// </summary>
        public event Action<float> onSliderValueUpdated;
        
        protected Slider slider;

        protected virtual void Awake() => slider = GetComponent<Slider>();

        protected virtual void OnEnable() => slider.onValueChanged.AddListener(OnSliderAction);

        protected virtual void OnDisable() => slider.onValueChanged.RemoveListener(OnSliderAction);

        protected virtual void OnSliderAction(float value)
        {
            OnSliderValueUpdated(value);
            onSliderValueUpdated?.Invoke(value);
        }
        
        /// <summary>
        /// Код, выполняемый при изменении значения слайдера
        /// </summary>
        public abstract void OnSliderValueUpdated(float value);
    }
}