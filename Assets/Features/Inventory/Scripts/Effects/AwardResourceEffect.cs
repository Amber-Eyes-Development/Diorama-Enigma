using System;
using DioramaEnigma.Sequences;
using Extensions.Helpers;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace DioramaEnigma.Inventory
{
    /// <summary>
    /// Эффект шага <see cref="SequenceStep"/>: добавить значение к ресурсу <see cref="ResourceValue"/> (выдать предмет)
    /// </summary>
    [Serializable]
    public sealed class AwardResourceEffect : SequenceStepEffect
    {
        [SerializeField] private ResourceValue resource;
        [Tooltip("Сколько добавить к ресурсу"), Min(1)]
        [SerializeField] private int valueToAdd = 1;

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Logic.IsNull(resource, nameof(resource))) return;

            resource.SetValue(resource.Value + valueToAdd);
        }
    }
}
