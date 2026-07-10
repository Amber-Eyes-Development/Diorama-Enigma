using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Serialization;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Композитный шаг: завершается по совокупности условий дочерних шагов
    /// </summary>
    [CreateAssetMenu(menuName = "StepSequences/Steps/Composite Step", fileName = nameof(CompositeSequenceStep))]
    public sealed class CompositeSequenceStep : AbstractSequenceStep
    {
        /// <inheritdoc/>
        public override bool IsCompleted => EvaluateCompleted();

        [Header("Композит"), Space]
        [Tooltip("Дочерние записи: шаг + условие, при котором он засчитывается")]
        [SerializeField] private CompositeStepEntry[] children;
        [Tooltip("Условие завершения по дочерним записям")]
        [SerializeField] private CompletionMode mode = CompletionMode.All;
        [Tooltip("Число N (для AtLeast, AtMost и Exactly)")]
        [Min(0)]
        [FormerlySerializedAs("atLeast")]
        [SerializeField] private int n = 1;

        /// <inheritdoc/>
        public override void ResetState()
        {
            foreach (var entry in Entries()) entry.Step.ResetState();

            NotifyCompletionChanged();
        }

        protected override void OnActiveChanged(bool active)
        {
            foreach (var entry in Entries())
            {
                var child = entry.Step;
                if (active)
                {
                    child.onCompletionChanged += OnChildStateChanged;
                    child.onUnlockChanged += OnChildStateChanged;
                }
                else
                {
                    child.onCompletionChanged -= OnChildStateChanged;
                    child.onUnlockChanged -= OnChildStateChanged;
                }
            }
        }

        #region Internal

        private void OnChildStateChanged(bool _) => NotifyCompletionChanged();

        private bool EvaluateCompleted()
        {
            int total = 0;
            int matched = 0;

            foreach (var entry in Entries())
            {
                total++;
                if (entry.IsSatisfied) matched++;
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
                    ServiceDebug.LogError(this, $"Необработанный {nameof(CompletionMode)}: {mode}");
                    return false;
            }
        }

        private IEnumerable<CompositeStepEntry> Entries()
        {
            if (children == null) yield break;

            foreach (var entry in children)
                if (entry.Step != null) yield return entry;
        }

        #endregion
    }
}
