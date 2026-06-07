using System;
using Extensions.Events;
using Extensions.Helpers;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Эффект шага последовательности, устанавливающий значение ресурса
    /// </summary>
    [Serializable]
    public sealed class AwardResourceEffect : SequenceStepEffect
    {
        [SerializeField] private BoolValue resource;
        [SerializeField] private bool valueToSet = true;

        /// <inheritdoc/>
        public override void Execute(EventHub hub)
        {
            if (Logic.IsNull(resource, nameof(resource))) return;

            resource.SetValue(valueToSet);
        }
    }
}
