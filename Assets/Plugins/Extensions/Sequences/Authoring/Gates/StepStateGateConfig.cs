using System;
using System.Collections.Generic;
using Extensions.Attributes;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Конфиг гейта по состоянию других шагов
    /// </summary>
    [Serializable]
    public sealed class StepStateGateConfig : GateConfig
    {
        [Tooltip("Состояние шагов, при котором гейт пройден")]
        [EnumRange(0, 9)]
        [SerializeField] private TriggerKind trigger = TriggerKind.Completed;
        [Tooltip("Шаги, которые должны быть в нужном состоянии")]
        [SerializeField] private StepDefinition[] requiredSteps;

        /// <inheritdoc/>
        public override Gate Build(BuildContext context)
        {
            var steps = new List<Step>();

            if (requiredSteps != null)
                foreach (var definition in requiredSteps)
                {
                    var step = context.GetOrBuild(definition);
                    if (step != null) steps.Add(step);
                }

            return new StepStateGate(trigger, steps);
        }
    }
}
