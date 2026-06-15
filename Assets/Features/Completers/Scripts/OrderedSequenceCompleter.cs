using System;
using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;
using DioramaEnigma.Sequences;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Завершитель: шаг завершается, когда шаги списка выполнены по очереди — от первого до последнего.
    /// Поведение при нарушении очереди задаётся <see cref="SequenceOrderMode"/>
    /// </summary>
    public sealed class OrderedSequenceCompleter : AbstractCompleter
    {
        [Tooltip("Шаги в требуемом порядке выполнения")]
        [SerializeField] private SequenceStep[] sequence;
        [Tooltip("Реакция на выполнение шага не по очереди")]
        [SerializeField] private SequenceOrderMode mode = SequenceOrderMode.RevertOutOfOrder;

        private SequenceStep[] steps;
        private Action<bool>[] handlers;

        /// <summary> Текущая позиция очереди (для <see cref="SequenceOrderMode.AutoCascade"/>) </summary>
        private int progress;
        /// <summary> Порядок выполнения по факту (для <see cref="SequenceOrderMode.CheckOnComplete"/>) </summary>
        private List<SequenceStep> completedOrder;

        private void OnEnable()
        {
            steps = FilterSteps();
            handlers = new Action<bool>[steps.Length];
            progress = 0;
            completedOrder = mode == SequenceOrderMode.CheckOnComplete ? SeedCompletedOrder() : null;

            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                handlers[i] = completed => OnStepChanged(step, completed);
                step.onCompletionChanged += handlers[i];
            }

            Recalculate(null, false);
        }

        private void OnDisable()
        {
            for (int i = 0; i < steps.Length; i++)
                steps[i].onCompletionChanged -= handlers[i];
        }

        #region Internal

        private void OnStepChanged(SequenceStep step, bool completed) => Recalculate(step, completed);

        private void Recalculate(SequenceStep changed, bool completed)
        {
            if (State == null || steps.Length == 0) return;

            switch (mode)
            {
                case SequenceOrderMode.RevertOutOfOrder: RecalculateRevert(); break;
                case SequenceOrderMode.AutoCascade: RecalculateCascade(); break;
                case SequenceOrderMode.CheckOnComplete: RecalculateCheck(changed, completed); break;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(SequenceOrderMode)}: {mode}");
                    break;
            }
        }

        /// <summary> Завершённые не по очереди шаги откатываются немедленно </summary>
        private void RecalculateRevert()
        {
            int k = 0;
            while (k < steps.Length && steps[k].IsCompleted) k++;

            for (int j = k + 1; j < steps.Length; j++)
                if (steps[j].IsCompleted)
                    steps[j].ForceValue(!steps[j].CompletionState);

            SetDone(k >= steps.Length);
        }

        /// <summary> Шаги, выполненные с опережением очереди, засчитываются, когда до них доходит черёд </summary>
        private void RecalculateCascade()
        {
            while (progress < steps.Length && steps[progress].IsCompleted)
                progress++;

            while (progress > 0 && !steps[progress - 1].IsCompleted)
                progress--;

            SetDone(progress >= steps.Length);
        }

        /// <summary> Любой порядок выполнения; по завершении всех — проверка очереди, иначе откат всех </summary>
        private void RecalculateCheck(SequenceStep changed, bool completed)
        {
            if (changed != null)
            {
                if (completed) completedOrder.Add(changed);
                else completedOrder.Remove(changed);
            }

            if (completedOrder.Count < steps.Length)
            {
                SetDone(false);
                return;
            }

            bool inOrder = true;
            for (int i = 0; i < steps.Length; i++)
            {
                if (completedOrder[i] != steps[i])
                {
                    inOrder = false;
                    break;
                }
            }

            if (inOrder)
            {
                SetDone(true);
                return;
            }

            completedOrder.Clear();
            foreach (var step in steps)
                if (step.IsCompleted) step.ForceValue(!step.CompletionState);

            SetDone(false);
        }

        private void SetDone(bool done) => State.ForceValue(done ? State.CompletionState : !State.CompletionState);

        private SequenceStep[] FilterSteps()
        {
            if (sequence == null) return Array.Empty<SequenceStep>();

            var result = new List<SequenceStep>(sequence.Length);
            foreach (var step in sequence)
                if (step != null) result.Add(step);

            return result.ToArray();
        }

        /// <summary> Предположить порядок уже выполненных шагов (после загрузки сейва) — по порядку списка </summary>
        private List<SequenceStep> SeedCompletedOrder()
        {
            var result = new List<SequenceStep>(steps.Length);
            foreach (var step in steps)
                if (step.IsCompleted) result.Add(step);

            return result;
        }

        #endregion
    }
}
