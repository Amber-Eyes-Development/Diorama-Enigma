using System;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Шаг-значение типа int (набор состояний): завершён, когда текущее состояние входит в допустимые.
    /// Наследует <see cref="IntValue"/> — встречается с системой взаимодействия на значении.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Steps/State Set Step", fileName = nameof(StateSetPuzzleStep))]
    public sealed class StateSetPuzzleStep : IntValue, IPuzzleStep
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
        [Tooltip("Индексы состояний, при которых шаг считается завершённым")]
        [SerializeField] private int[] acceptingStates = { 1 };

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
        public override void SetValue(int newValue)
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

        private bool EvaluateCompleted(int value)
        {
            if (acceptingStates == null) return false;

            foreach (int accepting in acceptingStates)
                if (accepting == value) return true;

            return false;
        }
    }
}
