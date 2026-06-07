using System;
using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.Serialization;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Шаг последовательности
    /// </summary>
    [CreateAssetMenu(menuName = "Sequences/Steps/Step", fileName = nameof(SequenceStep))]
    public sealed class SequenceStep : BoolValue, ISequenceStep
    {
        /// <inheritdoc/>
        public event Action<bool> onCompletionChanged
        {
            add => tracker.onCompletionChanged += value;
            remove => tracker.onCompletionChanged -= value;
        }
        /// <summary> Изменение разблокированности (можно ли менять состояние прямо сейчас) </summary>
        public event Action<bool> onUnlockChanged
        {
            add => tracker.onUnlockChanged += value;
            remove => tracker.onUnlockChanged -= value;
        }

        /// <summary> Метка шага </summary>
        public string StepLabel => stepLabel;
        /// <summary> Завершён ли шаг </summary>
        public bool IsCompleted => Value == completionState;
        /// <summary> Можно ли менять состояние шага прямо сейчас (группа доступна, гейт открыт, не залочен) </summary>
        public bool IsUnlocked => tracker.IsUnlocked;

        [Header("Шаг"), Space]
        [SerializeField] private string stepLabel;
        [SerializeField] private StepCompletionTracker tracker = new();

        [Header("Завершение"), Space]
        [Tooltip("Значение, при котором шаг считается завершённым")]
        [FormerlySerializedAs("completedWhen")]
        [SerializeField] private bool completionState = true;
        [Tooltip("Необратимый: после завершения состояние нельзя изменить обратно. " +
                 "Выкл — состояние можно менять в обе стороны")]
        [SerializeField] private bool irreversible;

        protected override void OnEnable()
        {
            base.OnEnable();
            tracker.Initialize(DefaultValue == completionState, irreversible);
        }

#if UNITY_EDITOR
        /// <summary> Новый шаг по умолчанию сохраняется между сессиями </summary>
        private void Reset() => isSaveable = true;

        protected override void OnValidate()
        {
            base.OnValidate();
            tracker.EditorValidate(this);
        }
#endif

        /// <inheritdoc/>
        public override void SetValue(bool newValue)
        {
            if (!tracker.IsUnlocked) return;

            base.SetValue(newValue);
            tracker.NotifyIfChanged(IsCompleted);
        }

        /// <inheritdoc/>
        public void SetActive(bool active) => tracker.SetActive(active);

        /// <inheritdoc/>
        public void ResetState()
        {
            base.SetValue(DefaultValue);
            tracker.NotifyIfChanged(IsCompleted);
        }
    }
}
