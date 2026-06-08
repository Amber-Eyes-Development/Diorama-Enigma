using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Композитный шаг: завершается по совокупности дочерних шагов
    /// </summary>
    [CreateAssetMenu(menuName = "Sequences/Steps/Composite Step", fileName = nameof(CompositeSequenceStep))]
    public sealed class CompositeSequenceStep : AbstractSequenceStep
    {
        /// <inheritdoc/>
        public override bool IsCompleted => EvaluateCompleted();

        [Header("Композит"), Space]
        [Tooltip("Дочерние шаги")]
        [SerializeField] private AbstractSequenceStep[] children;
        [Tooltip("Условие завершения по дочерним шагам")]
        [SerializeField] private CompletionMode mode = CompletionMode.All;
        [Tooltip("Минимум завершённых (только для AtLeast)")]
        [Min(1)]
        [SerializeField] private int atLeast = 1;

        /// <inheritdoc/>
        public override void ResetState()
        {
            foreach (var child in Children()) child.ResetState();

            NotifyCompletionChanged();
        }

        protected override void OnActiveChanged(bool active)
        {
            foreach (var child in Children())
            {
                if (active)
                {
                    child.SetActive(true);
                    child.onCompletionChanged += OnChildCompletionChanged;
                }
                else
                {
                    child.onCompletionChanged -= OnChildCompletionChanged;
                    child.SetActive(false);
                }
            }
        }

        #region Internal

        private void OnChildCompletionChanged(bool _) => NotifyCompletionChanged();

        private bool EvaluateCompleted()
        {
            int total = 0;
            int done = 0;

            foreach (var child in Children())
            {
                total++;
                if (child.IsCompleted) done++;
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

        private IEnumerable<AbstractSequenceStep> Children()
        {
            if (children == null) yield break;

            foreach (var child in children)
                if (child != null) yield return child;
        }

        #endregion
    }
}
