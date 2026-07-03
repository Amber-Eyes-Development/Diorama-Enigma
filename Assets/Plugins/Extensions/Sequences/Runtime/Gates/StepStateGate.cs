using System;
using System.Collections.Generic;
using Extensions.Helpers;

namespace Extensions.Sequences
{
    /// <summary>
    /// Гейт по состоянию других шагов: открыт, пока все требуемые шаги в нужном состоянии (триггере)
    /// </summary>
    public sealed class StepStateGate : Gate
    {
        /// <summary> Состояние шагов, при котором гейт пройден </summary>
        public TriggerKind Trigger => trigger;
        /// <summary> Требуемые шаги </summary>
        public IReadOnlyList<Step> RequiredSteps => requiredSteps;

        private readonly TriggerKind trigger;
        private readonly IReadOnlyList<Step> requiredSteps;
        private readonly List<ActionDisposable> subs = new();

        /// <summary> Новый гейт по состоянию шагов </summary>
        public StepStateGate(TriggerKind trigger, IReadOnlyList<Step> requiredSteps)
        {
            this.trigger = trigger;
            this.requiredSteps = requiredSteps ?? Array.Empty<Step>();
        }

        /// <inheritdoc/>
        public override bool IsSatisfied()
        {
            foreach (var step in requiredSteps)
            {
                if (step == null) continue;
                if (!Matches(step)) return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void StartObserving()
        {
            foreach (var step in requiredSteps)
            {
                if (step == null) continue;
                subs.Add(step.Completed.Subscribe(OnRequiredChanged));
                subs.Add(step.Unlocked.Subscribe(OnRequiredChanged));
            }
        }

        /// <inheritdoc/>
        public override void StopObserving()
        {
            foreach (var sub in subs) sub.Dispose();
            subs.Clear();
        }

        private bool Matches(Step step) =>
            trigger.IsChange() || trigger.IsSatisfiedBy(step.IsCompleted, step.IsUnlocked);

        private void OnRequiredChanged(bool _) => RaiseSatisfactionChanged();
    }
}
