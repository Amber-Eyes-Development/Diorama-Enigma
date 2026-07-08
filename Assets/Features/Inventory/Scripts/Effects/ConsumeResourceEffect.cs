using System;
using DioramaEnigma.Sequences;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Inventory
{
    /// <summary> Эффект шага <see cref="SequenceStep"/>: потратить (списать) количество ресурса <see cref="ResourceValue"/>
    /// (использование предмета) </summary>
    [Serializable]
    public sealed class ConsumeResourceEffect : SequenceStepEffect
    {
        [SerializeField] private ResourceValue resource;
        [Tooltip("Сколько списать с ресурса при срабатывании"), Min(1)]
        [SerializeField] private int amount = 1;

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Logic.IsNull(resource, nameof(resource))) return;

            resource.SetValue(Mathf.Max(0, resource.Value - amount));
        }
    }
}
