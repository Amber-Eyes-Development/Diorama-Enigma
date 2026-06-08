using Extensions.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Булев шаг: завершается, когда его значение совпадает с целевым.
    /// Сам хранит значение и (опционально) сохраняет его между сессиями.
    /// </summary>
    [CreateAssetMenu(menuName = "Sequences/Steps/Step", fileName = nameof(SequenceStep))]
    public sealed class SequenceStep : AbstractSequenceStep
    {
        /// <summary> Текущее значение </summary>
        public bool Value
        {
            get
            {
                LoadIfNeeded();
                return runtimeValue;
            }
        }
        /// <summary> Значение по умолчанию </summary>
        public bool DefaultValue => defaultValue;
        /// <summary> Целевое значение, при котором шаг считается завершённым </summary>
        public bool CompletionState => completionState;
        /// <summary> Глобальный профиль сохранения, иначе — отдельно для активного профиля </summary>
        public bool IsGlobal => isGlobal;

        /// <inheritdoc/>
        public override bool IsCompleted => Value == completionState;

        [Header("Значение"), Space]
        [Tooltip("Сохранять ли значение между сессиями")]
        [SerializeField] private bool isSaveable;
        [Tooltip("Глобальный профиль сохранения, иначе состояние отдельно для каждого активного профиля")]
        [SerializeField] private bool isGlobal;
        [Tooltip("Дефолтное значение, используемое если нет сохранения или оно отключено")]
        [SerializeField] private bool defaultValue;

        [Header("Завершение"), Space]
        [Tooltip("Значение, при котором шаг считается завершённым")]
        [FormerlySerializedAs("completedWhen")]
        [SerializeField] private bool completionState = true;

        private const string GLOBAL_PROFILE = "global values";

        [System.NonSerialized] private bool runtimeValue;
        private bool isLoaded;

        private string SaveProfile => isGlobal ? GLOBAL_PROFILE : null;

        /// <summary> Установить значение (если шаг разблокирован) </summary>
        public void SetValue(bool newValue)
        {
            if (!IsUnlocked) return;

            LoadIfNeeded();
            if (runtimeValue == newValue) return;

            ApplyValue(newValue);
        }

        /// <inheritdoc/>
        public override void ResetState()
        {
            LoadIfNeeded();
            if (runtimeValue != defaultValue)
                ApplyValue(defaultValue);
            else
                NotifyCompletionChanged();
        }

        protected override void OnEnable()
        {
            isLoaded = false;
            runtimeValue = defaultValue;
            base.OnEnable();
        }

        #region Internal

        private void ApplyValue(bool newValue)
        {
            runtimeValue = newValue;

            if (Application.isPlaying && isSaveable)
                JsonSaveLoad.Save(runtimeValue, Id, SaveProfile);

            NotifyCompletionChanged();
        }

        private void LoadIfNeeded()
        {
            if (!Application.isPlaying || isLoaded) return;
            isLoaded = true;

            runtimeValue = isSaveable
                ? JsonSaveLoad.Load(Id, defaultValue, SaveProfile)
                : defaultValue;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary> Новый шаг по умолчанию сохраняется между сессиями </summary>
        private void Reset() => isSaveable = true;
#endif
    }
}
