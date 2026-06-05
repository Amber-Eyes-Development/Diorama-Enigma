using System;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Шаг-значение типа string: завершён при совпадении с одним из допустимых значений
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Steps/String Step", fileName = nameof(StringPuzzleStep))]
    public sealed class StringPuzzleStep : StringValue, IPuzzleStep
    {
        /// <inheritdoc/>
        public event Action<bool> onCompletionChanged
        {
            add => tracker.onCompletionChanged += value;
            remove => tracker.onCompletionChanged -= value;
        }

        /// <summary> Метка шага </summary>
        public string StepLabel => stepLabel;

        /// <summary> Завершён ли шаг </summary>
        public bool IsCompleted => EvaluateCompleted(Value);

        [Header("Шаг"), Space]
        [SerializeField] private string stepLabel;
        [SerializeField] private StepCompletionTracker tracker = new();

        [Header("Завершение"), Space]
        [Tooltip("Значения, при любом из которых шаг считается завершённым")]
        [SerializeField] private string[] acceptedValues = Array.Empty<string>();
        [Tooltip("Игнорировать регистр при сравнении")]
        [SerializeField] private bool ignoreCase = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            tracker.Initialize(EvaluateCompleted(DefaultValue));
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            tracker.EditorValidate(this);
        }
#endif

        /// <inheritdoc/>
        public override void SetValue(string newValue)
        {
            // Блокировка изменения, пока шаг не активен / гейт закрыт
            if (!tracker.IsUnlocked) return;

            base.SetValue(newValue);
            tracker.NotifyIfChanged(IsCompleted);
        }

        /// <inheritdoc/>
        public void SetActive(bool active) => tracker.SetActive(active);

        /// <inheritdoc/>
        public void ResetState()
        {
            // Минуя veto: прямой вызов базового SetValue — уведомляет подписчиков и сбрасывает сейв
            base.SetValue(DefaultValue);
            tracker.NotifyIfChanged(IsCompleted);
        }

        private bool EvaluateCompleted(string value)
        {
            if (acceptedValues == null) return false;

            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            foreach (string accepted in acceptedValues)
                if (string.Equals(value, accepted, comparison)) return true;

            return false;
        }
    }
}
