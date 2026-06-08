using System;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Эффект шага: добавить значение к ресурсу
    /// </summary>
    [Serializable]
    public sealed class AwardResourceEffect : SequenceStepEffect
    {
        [SerializeField] private ResourceValue resource;
        [Tooltip("Сколько добавить к ресурсу (может быть отрицательным)")]
        [SerializeField] private int valueToAdd = 1;

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Logic.IsNull(resource, nameof(resource))) return;

            resource.SetValue(resource.Value + valueToAdd);
        }
    }
}
