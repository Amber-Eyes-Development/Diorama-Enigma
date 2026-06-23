using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.Reactive;

namespace Extensions.Sequences
{
    /// <summary>
    /// Движок последовательности: reconciler, а не stateful-степпер.
    /// Текущий этап ВЫВОДИТСЯ из состояний шагов; на изменения пересчитывается желаемый активный набор и диффится с текущим.
    /// </summary>
    /// <remarks>
    /// Следствия: резюм после загрузки бесплатен (этап выводится из восстановленного состояния),
    /// устойчивость к рантайм-мутации набора шагов (<see cref="Refresh"/>). Эффекты — edge-triggered через подписки.
    /// </remarks>
    public sealed class SequenceEngine
    {
        /// <summary> Последовательность завершена полностью </summary>
        public ReactiveEvent OnCompleted { get; } = new();

        /// <summary> Запущен ли движок </summary>
        public bool IsRunning => running;
        /// <summary> Завершена ли последовательность </summary>
        public bool IsCompleted => sequence.IsCompleted;
        /// <summary> Текущий этап (индекс ближайшей незавершённой не-ambient группы) </summary>
        public int CurrentStage => ComputeCurrentStage();

        private readonly Sequence sequence;
        private readonly SequencePersistence persistence;
        private readonly Dictionary<Step, StepEntry> entryOf = new();
        private readonly Dictionary<Step, List<ActionDisposable>> subs = new();
        private readonly HashSet<Step> active = new();

        private bool running;
        private bool completedFired;

        /// <summary> Новый движок. persistence опционален: если задан — снапшот сохраняется на изменениях </summary>
        public SequenceEngine(Sequence sequence, SequencePersistence persistence = null)
        {
            this.sequence = sequence;
            this.persistence = persistence;
            IndexEntries();
        }

        #region Lifecycle

        /// <summary> Запустить движок (reconcile выведет этап из текущего состояния) </summary>
        public void Start()
        {
            if (running) Stop();
            running = true;
            completedFired = false;
            Reconcile();
        }

        /// <summary> Остановить движок и снять подписки </summary>
        public void Stop()
        {
            var current = new List<Step>(active);
            foreach (var step in current) Deactivate(step);
            active.Clear();
            running = false;
        }

        /// <summary> Сбросить все шаги к исходному и пересогласовать </summary>
        public void Reset()
        {
            foreach (var entry in sequence.Steps)
                entry?.Step?.ResetState();

            completedFired = false;
            Reconcile();
            Save();
        }

        /// <summary> Пересобрать индекс записей и пересогласовать (после мутации набора шагов) </summary>
        public void Refresh()
        {
            IndexEntries();
            Reconcile();
        }

        #endregion

        #region Reconcile

        private void Reconcile()
        {
            if (!running) return;

            // Фикспоинт: активация может довершить группу (триггеры по разблокировке) → этап сдвигается
            int guard = 0;
            while (ReconcileOnce() && guard++ < 64) { }

            if (!completedFired && sequence.IsCompleted)
            {
                completedFired = true;
                OnCompleted.Invoke();
            }
        }

        private bool ReconcileOnce()
        {
            var desired = ComputeDesiredActive();
            bool changed = false;

            var current = new List<Step>(active);
            foreach (var step in current)
                if (!desired.Contains(step))
                {
                    Deactivate(step);
                    changed = true;
                }

            foreach (var step in desired)
                if (!active.Contains(step))
                {
                    Activate(step);
                    changed = true;
                }

            return changed;
        }

        private HashSet<Step> ComputeDesiredActive()
        {
            int stage = ComputeCurrentStage();
            var set = new HashSet<Step>();

            foreach (var entry in sequence.Steps)
            {
                var step = entry?.Step;
                if (step == null) continue;

                if (IsAmbient(entry.GroupIndex))
                {
                    set.Add(step);
                    continue;
                }

                if (entry.GroupIndex == stage)
                    set.Add(step);
                else if (entry.GroupIndex < stage && entry.InteractableAfterCompletion)
                    set.Add(step);
            }

            return set;
        }

        private int ComputeCurrentStage()
        {
            int max = -1;
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && !IsAmbient(entry.GroupIndex) && entry.GroupIndex > max)
                    max = entry.GroupIndex;

            for (int group = 0; group <= max; group++)
            {
                if (IsAmbient(group)) continue;
                if (!GroupHasSteps(group)) continue;
                if (!IsGroupCompleted(group)) return group;
            }

            return max + 1;
        }

        private bool IsAmbient(int group) => sequence.AvailabilityOf(group) == GroupAvailability.Always;

        private bool GroupHasSteps(int group)
        {
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.GroupIndex == group)
                    return true;

            return false;
        }

        private bool IsGroupCompleted(int group)
        {
            bool any = false;
            foreach (var entry in sequence.Steps)
            {
                if (entry?.Step == null || entry.GroupIndex != group) continue;
                any = true;
                if (!entry.Step.IsCompleted) return false;
            }

            return any;
        }

        #endregion

        #region Activation

        private void Activate(Step step)
        {
            if (!entryOf.TryGetValue(step, out var entry)) return;

            step.Activate(entry.Gates);

            var list = new List<ActionDisposable>(3)
            {
                step.Completed.Subscribe(completed => OnStepCompletionChanged(entry, completed)),
                step.Unlocked.Subscribe(unlocked => OnStepUnlockChanged(entry, unlocked)),
                step.StateChanged.Subscribe(Save),
            };
            subs[step] = list;
            active.Add(step);
        }

        private void Deactivate(Step step)
        {
            if (subs.TryGetValue(step, out var list))
            {
                foreach (var sub in list) sub.Dispose();
                subs.Remove(step);
            }

            step.Deactivate();
            active.Remove(step);
        }

        private void OnStepCompletionChanged(StepEntry entry, bool completed)
        {
            RunEffects(entry, completed ? TriggerKind.Completed : TriggerKind.NotCompleted);
            Reconcile();
            Save();
        }

        private void OnStepUnlockChanged(StepEntry entry, bool unlocked)
        {
            RunEffects(entry, unlocked ? TriggerKind.Unlocked : TriggerKind.Locked);
        }

        private static void RunEffects(StepEntry entry, TriggerKind fired)
        {
            foreach (var effectEntry in entry.Effects)
                if (effectEntry != null && effectEntry.Trigger.Responds(fired))
                    effectEntry.Effect?.Execute();
        }

        private void Save() => persistence?.Save(sequence);

        private void IndexEntries()
        {
            entryOf.Clear();
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null)
                    entryOf[entry.Step] = entry;
        }

        #endregion
    }
}
