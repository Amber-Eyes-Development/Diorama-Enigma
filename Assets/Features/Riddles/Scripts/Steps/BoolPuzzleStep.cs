using System;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Шаг-значение типа bool: завершён при совпадении с целевым значением.
    /// Наследует <see cref="BoolValue"/> — встречается с системой взаимодействия на значении.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Steps/Bool Step", fileName = nameof(BoolPuzzleStep))]
    public sealed class BoolPuzzleStep : BoolValue, IPuzzleStep
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
        public bool IsCompleted => Value == completedWhen;

        [Header("Шаг"), Space]
        [SerializeField] private string stepLabel;
        [SerializeField] private StepCompletionTracker tracker = new();

        [Header("Завершение"), Space]
        [Tooltip("Значение, при котором шаг считается завершённым")]
        [SerializeField] private bool completedWhen = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            tracker.Initialize(DefaultValue == completedWhen);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            tracker.EditorValidate(this);
        }
#endif

        /// <inheritdoc/>
        public override void SetValue(bool newValue)
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
    }
}
