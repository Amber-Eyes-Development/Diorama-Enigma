using System;

namespace Extensions.Sequences
{
    /// <summary>
    /// Счётчик: завершён, когда значение достигает порога (напр. «собрать 10»).
    /// Пример не-булевой обобщённости модели.
    /// </summary>
    public sealed class CounterStep : Step
    {
        /// <summary> Текущее значение </summary>
        public int Count => count;
        /// <summary> Порог завершения </summary>
        public int Target => target;

        private readonly int target;
        private readonly int defaultCount;
        private int count;

        /// <summary> Новый счётчик </summary>
        public CounterStep(string id, int target, int defaultCount = 0, bool irreversible = false)
            : base(id, irreversible)
        {
            this.target = target;
            this.defaultCount = Math.Max(0, defaultCount);
            count = this.defaultCount;
            InitializeCompletion();
        }

        /// <summary> Прибавить к счётчику (если шаг разблокирован) </summary>
        public void Add(int delta = 1) => SetCount(count + delta);

        /// <summary> Задать значение, если шаг разблокирован; иначе оповестить об отклонённой попытке </summary>
        public void SetCount(int newCount)
        {
            if (!IsUnlocked)
            {
                NotifyInteractionRejected();
                return;
            }

            ForceCount(newCount);
        }

        /// <summary> Задать значение, минуя проверку разблокировки </summary>
        public void ForceCount(int newCount)
        {
            newCount = Math.Max(0, newCount);
            if (count == newCount) return;

            count = newCount;
            RefreshCompletion();
        }

        /// <inheritdoc/>
        public override void ResetState() => ForceCount(defaultCount);

        /// <inheritdoc/>
        protected override bool EvaluateCompleted() => count >= target;

        internal int CaptureCount() => count;

        internal void RestoreCount(int newCount)
        {
            count = Math.Max(0, newCount);
            RefreshCompletion();
        }
    }
}
