using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [Tooltip("Число N (для AtLeast, AtMost и Exactly)")]
        [Min(0)]
        [FormerlySerializedAs("atLeast")]
        [SerializeField] private int n = 1;

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
                CompletionMode.AtLeast => done >= n,
                CompletionMode.AtMost => done <= n,
                CompletionMode.Exactly => done == n,
                CompletionMode.None => done == 0,
                CompletionMode.NotAll => done < total,
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
