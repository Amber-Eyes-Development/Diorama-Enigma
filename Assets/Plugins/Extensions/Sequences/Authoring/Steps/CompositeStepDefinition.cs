using System;
using System.Collections.Generic;
using Extensions.Attributes;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Определение композитного шага
    /// </summary>
    [CreateAssetMenu(menuName = "Extensions/Sequences/Steps/Composite Step", fileName = nameof(CompositeStepDefinition))]
    public sealed class CompositeStepDefinition : StepDefinition
    {
        /// <summary> Дочерняя запись: шаг + условие засчитывания </summary>
        [Serializable]
        private struct ChildConfig
        {
            public StepDefinition step;
            [EnumRange(0, 9)] public TriggerKind trigger;
        }

        [Header("Композит"), Space]
        [Tooltip("Дочерние записи: шаг + условие, при котором он засчитывается")]
        [SerializeField] private ChildConfig[] children = Array.Empty<ChildConfig>();
        [Tooltip("Режим завершения по дочерним записям")]
        [SerializeField] private CompletionMode mode = CompletionMode.All;
        [Tooltip("Число N (для AtLeast/AtMost/Exactly)")]
        [Min(0)]
        [SerializeField] private int n = 1;

        /// <inheritdoc/>
        public override Step Build(BuildContext context)
        {
            var list = new List<CompositeStep.Child>();

            if (children != null)
                foreach (var child in children)
                {
                    var step = context.GetOrBuild(child.step);
                    if (step != null) list.Add(new CompositeStep.Child(step, child.trigger));
                }

            return new CompositeStep(Id, mode, n, list, Irreversible);
        }
    }
}
