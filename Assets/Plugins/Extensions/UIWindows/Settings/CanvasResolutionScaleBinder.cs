using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Связка CanvasScaler с FloatValue для масштабирования интерфейса
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasResolutionScaleBinder : MonoBehaviour
    {
        private const float MIN_SCALE = 0.5f;
        private const float MAX_SCALE = 1.5f;
        
        [Tooltip("Значение масштаба интерфейса")]
        [SerializeField] private FloatValue scaleValue;
        
        private readonly Vector2 baseReferenceResolution = new(2560f, 1480f);

        private CanvasScaler canvasScaler;

        private void Awake() => canvasScaler = GetComponent<CanvasScaler>();

        private void OnEnable()
        {
            if (scaleValue == null)
            {
                Apply(1f);
                return;
            }

            scaleValue.onValueChanged += OnScaleChanged;
            Apply(scaleValue.Value);
        }

        private void OnDisable()
        {
            if (scaleValue == null) return;

            scaleValue.onValueChanged -= OnScaleChanged;
        }

        /// <summary>
        /// Применить текущее значение масштаба "вручную"
        /// </summary>
        public void ApplyCurrentValue()
        {
            if (scaleValue == null)
            {
                Apply(1f);
                return;
            }

            Apply(scaleValue.Value);
        }

        private void OnScaleChanged(float value) => Apply(value);

        private void Apply(float value)
        {
            float scale = Mathf.Clamp(value, MIN_SCALE, MAX_SCALE);

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = baseReferenceResolution / scale;
        }
    }
}