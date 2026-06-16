using System;
using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;
using DioramaEnigma.Sequences;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Завершитель: шаг завершается, когда шаги списка выполнены по очереди — от первого до последнего
    /// </summary>
    public sealed class OrderedSequenceCompleter : AbstractCompleter
    {
        [Tooltip("Шаги в требуемом порядке выполнения")]
        [SerializeField] private SequenceStep[] sequence;
        [Tooltip("Реакция на выполнение шага не по очереди")]
        [SerializeField] private SequenceOrderMode mode = SequenceOrderMode.RevertOutOfOrder;

        private SequenceStep[] steps;
        private Action<bool>[] handlers;

        private bool isDirty;
        private bool isCommitting;

        /// <summary> Текущая позиция очереди (для <see cref="SequenceOrderMode.AutoCascade"/>) </summary>
        private int progress;
        /// <summary> Порядок выполнения по факту (для <see cref="SequenceOrderMode.CheckOnComplete"/>) </summary>
        private List<SequenceStep> completedOrder;
        /// <summary> Изменения шагов в порядке поступления (для <see cref="SequenceOrderMode.CheckOnComplete"/>) </summary>
        private readonly List<(SequenceStep step, bool completed)> pendingChanges = new();

        private void OnEnable()
        {
            steps = FilterSteps();
            handlers = new Action<bool>[steps.Length];
            progress = 0;
            isCommitting = false;
            pendingChanges.Clear();
            completedOrder = mode == SequenceOrderMode.CheckOnComplete ? SeedCompletedOrder() : null;

            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                handlers[i] = completed => OnStepChanged(step, completed);
                step.onCompletionChanged += handlers[i];
            }

            isDirty = true;
        }

        private void OnDisable()
        {
            for (int i = 0; i < steps.Length; i++)
                steps[i].onCompletionChanged -= handlers[i];
        }

        private void LateUpdate()
        {
            if (!isDirty) return;

            isDirty = false;
            Commit();
        }

        #region Internal

        private void OnStepChanged(SequenceStep step, bool completed)
        {
            if (isCommitting) return;

            if (mode == SequenceOrderMode.CheckOnComplete)
                pendingChanges.Add((step, completed));

            isDirty = true;
        }

        private void Commit()
        {
            if (State == null || steps.Length == 0)
            {
                pendingChanges.Clear();
                return;
            }

            isCommitting = true;
            try
            {
                switch (mode)
                {
                    case SequenceOrderMode.RevertOutOfOrder: RecalculateRevert(); break;
                    case SequenceOrderMode.AutoCascade: RecalculateCascade(); break;
                    case SequenceOrderMode.CheckOnComplete: RecalculateCheck(); break;
                    default:
                        ServiceDebug.LogError(this, $"Необработанный {nameof(SequenceOrderMode)}: {mode}");
                        break;
                }
            }
            finally
            {
                isCommitting = false;
            }
        }

        /// <summary> Завершённые не по очереди шаги откатываются </summary>
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
        private void RecalculateCheck()
        {
            foreach (var (step, completed) in pendingChanges)
            {
                if (completed) completedOrder.Add(step);
                else completedOrder.Remove(step);
            }
            pendingChanges.Clear();

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
