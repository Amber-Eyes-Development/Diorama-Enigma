using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.Helpers.Enumerations;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Завершитель: значение шага = логическая функция (И/ИЛИ) операндов
    /// </summary>
    public sealed class LogicCompleter : AbstractCompleter
    {
        [Tooltip("Как объединять операнды")]
        [SerializeField] private LogicOperator operatorKind = LogicOperator.And;
        [Tooltip("Операнды: шаг + условие, при котором он считается истинным")]
        [SerializeField] private CompleterEntry[] operands;

        private void OnEnable()
        {
            foreach (var entry in Operands())
            {
                entry.Step.onCompletionChanged += OnOperandChanged;
                entry.Step.onUnlockChanged += OnOperandChanged;
            }

            Recalculate();
        }

        private void OnDisable()
        {
            foreach (var entry in Operands())
            {
                entry.Step.onCompletionChanged -= OnOperandChanged;
                entry.Step.onUnlockChanged -= OnOperandChanged;
            }
        }

        #region Internal

        private void OnOperandChanged(bool _) => Recalculate();

        private void Recalculate()
        {
            if (State == null) return;

            bool result = Evaluate();
            State.ForceValue(result ? State.CompletionState : !State.CompletionState);
        }

        private bool Evaluate()
        {
            int total = 0;
            int matched = 0;

            foreach (var entry in Operands())
            {
                total++;
                if (entry.IsSatisfied) matched++;
            }

            if (total == 0) return false;

            switch (operatorKind)
            {
                case LogicOperator.And: return matched == total;
                case LogicOperator.Or: return matched > 0;
                case LogicOperator.Not: return matched == 0;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(LogicOperator)}: {operatorKind}");
                    return false;
            }
        }

        private IEnumerable<CompleterEntry> Operands()
        {
            if (operands == null) yield break;

            foreach (var entry in operands)
                if (entry.Step != null) yield return entry;
        }

        #endregion
    }
}
