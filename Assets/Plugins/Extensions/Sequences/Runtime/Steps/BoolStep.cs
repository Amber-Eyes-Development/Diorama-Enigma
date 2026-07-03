namespace Extensions.Sequences
{
    /// <summary>
    /// Булев шаг: завершён, когда значение совпадает с целевым
    /// </summary>
    public sealed class BoolStep : Step
    {
        /// <summary> Текущее значение </summary>
        public bool Value => value;
        /// <summary> Целевое значение, при котором шаг завершён </summary>
        public bool CompletionState => completionState;

        private readonly bool completionState;
        private readonly bool defaultValue;
        private bool value;

        /// <summary> Новый булев шаг </summary>
        public BoolStep(string id, bool completionState = true, bool defaultValue = false, bool irreversible = false)
            : base(id, irreversible)
        {
            this.completionState = completionState;
            this.defaultValue = defaultValue;
            value = defaultValue;
            InitializeCompletion();
        }

        /// <summary> Установить значение, если шаг разблокирован; иначе оповестить об отклонённой попытке </summary>
        public void SetValue(bool newValue)
        {
            if (!IsUnlocked)
            {
                NotifyInteractionRejected();
                return;
            }

            ForceValue(newValue);
        }

        /// <summary> Установить значение, минуя проверку разблокировки (оркестрация/эффекты) </summary>
        public void ForceValue(bool newValue)
        {
            if (value == newValue) return;

            value = newValue;
            RefreshCompletion();
        }

        /// <inheritdoc/>
        public override void ResetState() => ForceValue(defaultValue);

        /// <inheritdoc/>
        protected override bool EvaluateCompleted() => value == completionState;

        internal bool CaptureValue() => value;

        internal void RestoreValue(bool newValue)
        {
            value = newValue;
            RefreshCompletion();
        }
    }
}
