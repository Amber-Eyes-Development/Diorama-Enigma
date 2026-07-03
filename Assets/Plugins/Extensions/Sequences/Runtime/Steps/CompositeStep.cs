using System;
using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.Log;

namespace Extensions.Sequences
{
    /// <summary>
    /// Композитный шаг: завершается по булевой комбинации состояний дочерних шагов
    /// </summary>
    public sealed class CompositeStep : Step
    {
        /// <summary> Дочерняя запись: шаг + условие, при котором он засчитывается </summary>
        public readonly struct Child
        {
            /// <summary> Дочерний шаг </summary>
            public Step Step { get; }
            /// <summary> Состояние шага, при котором запись засчитывается </summary>
            public TriggerKind Trigger { get; }

            /// <summary> Новая дочерняя запись </summary>
            public Child(Step step, TriggerKind trigger)
            {
                Step = step;
                Trigger = trigger;
            }

            /// <summary> Выполнено ли условие записи прямо сейчас </summary>
            public bool IsSatisfied => Step != null && Trigger.IsSatisfiedBy(Step.IsCompleted, Step.IsUnlocked);
        }

        /// <summary> Дочерние записи </summary>
        public IReadOnlyList<Child> Children => children;
        /// <summary> Режим завершения </summary>
        public CompletionMode Mode => mode;
        /// <summary> Число N (для AtLeast/AtMost/Exactly) </summary>
        public int N => n;

        private readonly IReadOnlyList<Child> children;
        private readonly CompletionMode mode;
        private readonly int n;
        private readonly List<ActionDisposable> childSubs = new();

        /// <summary> Новый композитный шаг </summary>
        public CompositeStep(string id, CompletionMode mode, int n, IReadOnlyList<Child> children, bool irreversible = false)
            : base(id, irreversible)
        {
            this.mode = mode;
            this.n = n;
            this.children = children ?? Array.Empty<Child>();
            InitializeCompletion();
        }

        /// <inheritdoc/>
        public override void ResetState()
        {
            foreach (var child in children)
                child.Step?.ResetState();

            RefreshCompletion();
        }

        /// <inheritdoc/>
        protected override void OnActiveChanged(bool active)
        {
            if (active)
            {
                foreach (var child in children)
                {
                    var step = child.Step;
                    if (step == null) continue;

                    step.Activate(null);
                    childSubs.Add(step.Completed.Subscribe(OnChildChanged));
                    childSubs.Add(step.Unlocked.Subscribe(OnChildChanged));
                }
            }
            else
            {
                foreach (var sub in childSubs) sub.Dispose();
                childSubs.Clear();

                foreach (var child in children)
                    child.Step?.Deactivate();
            }

            RefreshCompletion();
        }

        /// <inheritdoc/>
        protected override bool EvaluateCompleted()
        {
            int total = 0;
            int matched = 0;

            foreach (var child in children)
            {
                if (child.Step == null) continue;
                total++;
                if (child.IsSatisfied) matched++;
            }

            if (total == 0) return false;

            switch (mode)
            {
                case CompletionMode.All: return matched == total;
                case CompletionMode.Any: return matched > 0;
                case CompletionMode.AtLeast: return matched >= n;
                case CompletionMode.AtMost: return matched <= n;
                case CompletionMode.Exactly: return matched == n;
                case CompletionMode.None: return matched == 0;
                case CompletionMode.NotAll: return matched < total;
                default:
                    ServiceDebug.LogError<CompositeStep>($"Необработанный {nameof(CompletionMode)}: {mode}");
                    return false;
            }
        }

        private void OnChildChanged(bool _) => RefreshCompletion();
    }
}
