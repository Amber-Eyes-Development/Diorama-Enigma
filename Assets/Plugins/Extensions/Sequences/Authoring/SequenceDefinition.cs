using System;
using System.Collections.Generic;
using Extensions.Identification;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Авторский ассет последовательности: строит рантайм-<see cref="Sequence"/> (POCO). Id берётся из ассета.
    /// </summary>
    [CreateAssetMenu(menuName = "Extensions/Sequences/Sequence", fileName = nameof(SequenceDefinition))]
    public sealed class SequenceDefinition : IdentifiableObject
    {
        [Header("Последовательность"), Space]
        [Tooltip("Записи шагов (группируются по индексу группы)")]
        [SerializeField] private StepEntryConfig[] entries = Array.Empty<StepEntryConfig>();

        /// <summary> Построить рантайм-последовательность </summary>
        public Sequence Build(BuildContext context = null)
        {
            context ??= new BuildContext();

            var built = new List<StepEntry>();
            if (entries != null)
                foreach (var entry in entries)
                {
                    var stepEntry = entry?.Build(context);
                    if (stepEntry != null) built.Add(stepEntry);
                }

            return new Sequence(Id, built);
        }
    }
}
