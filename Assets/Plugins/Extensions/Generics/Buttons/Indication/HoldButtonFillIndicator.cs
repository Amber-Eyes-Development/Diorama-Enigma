using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Индикатор зажатия кнопки <see cref="HoldButtonClickOrchestrator"/>
    /// </summary>
    [RequireComponent(typeof(HoldButtonClickOrchestrator))]
    public class HoldButtonFillIndicator : MonoBehaviour
    {
        [SerializeField]
        protected Image fillImage = default;
        
        protected HoldButtonClickOrchestrator holdButton = default;

        protected virtual void Awake()
        {
            holdButton = GetComponent<HoldButtonClickOrchestrator>();
        }
        
        protected virtual void OnEnable()
        {
            if (!holdButton) return;
            
            holdButton.onHoldStarted += OnHoldStarted;
            holdButton.onHoldProgressChanged += OnHoldProgressChanged;
            holdButton.onHoldCanceled += OnHoldCanceled;
            holdButton.onHoldCompleted += OnHoldCompleted;

            SetFill(0f);
        }

        protected virtual void OnDisable()
        {
            if (!holdButton) return;
            
            holdButton.onHoldStarted -= OnHoldStarted;
            holdButton.onHoldProgressChanged -= OnHoldProgressChanged;
            holdButton.onHoldCanceled -= OnHoldCanceled;
            holdButton.onHoldCompleted -= OnHoldCompleted;
        }

        protected virtual void OnHoldStarted()
        {
            SetFill(0f);
            fillImage.CrossFadeAlpha(1f, 0.1f, false);
        }

        protected virtual void OnHoldProgressChanged(float progress) => SetFill(progress);

        protected virtual void OnHoldCanceled() => SetFill(0f);

        protected virtual void OnHoldCompleted()
        {
            SetFill(1f);
            fillImage.CrossFadeAlpha(0, 1f, true);
        }

        protected void SetFill(float value)
        {
            if (!fillImage) return;

            if (value < 0f) value = 0f;
            if (value > 1f) value = 1f;

            fillImage.fillAmount = value;
        }
    }
}