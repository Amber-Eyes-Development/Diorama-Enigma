using System;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Эффект шага: принудительно задать значение другому булеву шагу <see cref="SequenceStep"/>
    /// </summary>
    [Serializable]
    public sealed class SetStepStateEffect : SequenceStepEffect
    {
        [Tooltip("Шаг, состояние которого нужно изменить")]
        [SerializeField] private SequenceStep targetStep;
        [Tooltip("Устанавливаемое значение")]
        [SerializeField] private bool value = true;

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Logic.IsNull(targetStep, nameof(targetStep))) return;

            targetStep.ForceValue(value);
        }
    }
}
