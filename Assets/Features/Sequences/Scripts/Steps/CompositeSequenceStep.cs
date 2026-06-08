using System;
using System.Collections.Generic;
using Extensions.Identification;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Композитный шаг (мульти-шаг): завершается по совокупности дочерних шагов
    /// </summary>
    [CreateAssetMenu(menuName = "Sequences/Steps/Composite Step", fileName = nameof(CompositeSequenceStep))]
    public sealed class CompositeSequenceStep : IdentifiableObject, ISequenceStep
    {
        /// <summary> Изменение признака завершённости </summary>
        public event Action<bool> onCompletionChanged;

        /// <summary> Метка шага </summary>
        public string StepLabel => stepLabel;
        /// <summary> Завершён ли шаг </summary>
        public bool IsCompleted => EvaluateCompleted();

        [Header("Шаг"), Space]
        [SerializeField] private string stepLabel;
        [Tooltip("Дочерние шаги (ссылки на ассеты-шаги)")]
        [SequenceStepReference]
        [SerializeField] private IdentifiableObject[] children;
        [Tooltip("Условие завершения по дочерним шагам")]
        [SerializeField] private CompletionMode mode = CompletionMode.All;
        [Tooltip("Минимум завершённых (только для AtLeast)")]
        [Min(1)]
        [SerializeField] private int atLeast = 1;

        private bool subscribed;
        private bool lastCompleted;

        private void OnEnable() => subscribed = false;

        private void OnDisable() => Unsubscribe();

        /// <inheritdoc/>
        public void SetActive(bool active)
        {
            if (active)
            {
                Subscribe();
                foreach (var child in EnumerateSteps()) child.SetActive(true);
            }
            else
            {
                foreach (var child in EnumerateSteps()) child.SetActive(false);
                Unsubscribe();
            }
        }

        /// <inheritdoc/>
        public void ResetState()
        {
            foreach (var child in EnumerateSteps()) child.ResetState();

            lastCompleted = EvaluateCompleted();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (children == null) return;

            foreach (var child in children)
                if (child != null && child is not ISequenceStep)
                    ServiceDebug.LogWarning(this, $"Дочерний объект «{child.name}» не является шагом ({nameof(SequenceStep)})");
        }
#endif

        #region Internal

        private void Subscribe()
        {
            if (subscribed) return;
            subscribed = true;

            lastCompleted = EvaluateCompleted();

            foreach (var child in EnumerateSteps())
                child.onCompletionChanged += OnChildCompletionChanged;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            subscribed = false;

            foreach (var child in EnumerateSteps())
                child.onCompletionChanged -= OnChildCompletionChanged;
        }

        private void OnChildCompletionChanged(bool _)
        {
            bool completed = EvaluateCompleted();
            if (completed == lastCompleted) return;

            lastCompleted = completed;
            onCompletionChanged?.Invoke(completed);
        }

        private bool EvaluateCompleted()
        {
            int total = 0;
            int done = 0;

            foreach (var step in EnumerateSteps())
            {
                total++;
                if (step.IsCompleted) done++;
            }

            if (total == 0) return false;

            return mode switch
            {
                CompletionMode.All => done == total,
                CompletionMode.Any => done > 0,
                CompletionMode.AtLeast => done >= atLeast,
                _ => false,
            };
        }

        private IEnumerable<ISequenceStep> EnumerateSteps()
        {
            if (children == null) yield break;

            foreach (var child in children)
                if (child is ISequenceStep step)
                    yield return step;
        }

        #endregion
    }
}
