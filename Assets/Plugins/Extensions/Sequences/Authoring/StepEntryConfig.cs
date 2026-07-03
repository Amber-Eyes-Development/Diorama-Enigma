using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Авторская запись шага: ссылка на definition + группа + доступность + инлайн гейты/эффекты
    /// </summary>
    [Serializable]
    public sealed class StepEntryConfig
    {
        [Tooltip("Определение шага")]
        [SerializeField] private StepDefinition step;
        [Tooltip("Шаги с одинаковым индексом активируются одновременно; следующая группа стартует после завершения текущей")]
        [SerializeField] private int groupIndex;
        [Tooltip("Доступность группы: с самого старта (Always) или только после предыдущих групп")]
        [SerializeField] private GroupAvailability availability;
        [Tooltip("Остаётся ли группа интерактивной после завершения")]
        [SerializeField] private bool interactableAfterCompletion;
        [Tooltip("Условия доступа (помимо порядка групп): все должны быть выполнены")]
        [SerializeReference] private GateConfig[] gates = Array.Empty<GateConfig>();
        [Tooltip("Эффекты записи (каждый со своим триггером)")]
        [SerializeField] private EffectEntryConfig[] effects = Array.Empty<EffectEntryConfig>();

        /// <summary> Построить рантайм-запись (или null, если шаг не задан) </summary>
        public StepEntry Build(BuildContext context)
        {
            var builtStep = context.GetOrBuild(step);
            if (builtStep == null) return null;

            var builtGates = new List<Gate>();
            if (gates != null)
                foreach (var gate in gates)
                {
                    var built = gate?.Build(context);
                    if (built != null) builtGates.Add(built);
                }

            var builtEffects = new List<EffectEntry>();
            if (effects != null)
                foreach (var effectConfig in effects)
                {
                    var built = effectConfig?.Build(context);
                    if (built != null) builtEffects.Add(built);
                }

            return new StepEntry(builtStep, groupIndex, availability, interactableAfterCompletion, builtGates, builtEffects);
        }
    }
}
