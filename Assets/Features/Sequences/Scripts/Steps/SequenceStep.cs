using Extensions.Attributes;
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
        /// <summary> Идёт ли кулдаун переключения прямо сейчас (значение менять нельзя) </summary>
        public bool IsOnCooldown => Application.isPlaying && Time.time - lastChangeTime < toggleCooldown;
        /// <summary> Можно ли сейчас изменить значение взаимодействием: шаг разблокирован и не на кулдауне
        /// (для жестов, решающих, стоит ли вообще начинать — напр. подхват драга) </summary>
        public bool CanChangeValue => IsUnlocked && !IsOnCooldown;

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

        [Header("Переключение"), Space]
        [Tooltip("Защита от дребезга: сколько секунд после смены значения нельзя менять его снова")]
        [SoftRange(0f, 1f, 1)]
        [SerializeField] private float toggleCooldown = 0.5f;

        private const string GLOBAL_PROFILE = "global values";

        [System.NonSerialized] private bool runtimeValue;
        [System.NonSerialized] private float lastChangeTime;
        private bool isLoaded;

        private string SaveProfile => isGlobal ? GLOBAL_PROFILE : null;

        protected override void OnEnable()
        {
            isLoaded = false;
            runtimeValue = defaultValue;
            lastChangeTime = float.NegativeInfinity;
            base.OnEnable();
        }
        
        /// <summary> Установить значение, если шаг это допускает (разблокировка + кулдаун); иначе оповестить об отклонённой попытке </summary>
        /// <param name="newValue">Новое значение</param>
        /// <param name="notify">Оповещать ли об отклонённой попытке (фидбэк вью; комплитерам не нужно)</param>
        /// <param name="bypassCooldown">Пропустить кулдаун (для физических событий — не пользовательского дребезга)</param>
        public void SetValue(bool newValue, bool notify = true, bool bypassCooldown = false)
        {
            if (!IsUnlocked || (!bypassCooldown && IsOnCooldown))
            {
                if (notify) NotifyInteractionRejected();
                return;
            }

            LoadIfNeeded();
            if (runtimeValue == newValue) return;

            ApplyValue(newValue);
            if (!bypassCooldown) lastChangeTime = Application.isPlaying ? Time.time : 0f;
        }

        /// <summary> Принудительно установить значение, минуя проверку разблокировки (для оркестрации/эффектов) </summary>
        public void ForceValue(bool newValue)
        {
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
