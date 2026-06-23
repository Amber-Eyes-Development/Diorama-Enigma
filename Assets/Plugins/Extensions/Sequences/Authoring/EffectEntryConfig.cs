using System;
using Extensions.Attributes;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Авторская запись эффекта: триггер (событие шага) + конфиг эффекта (инлайн, полиморфно)
    /// </summary>
    [Serializable]
    public sealed class EffectEntryConfig
    {
        [Tooltip("Событие шага, на которое реагирует эффект")]
        [EnumRange(0, 9)]
        [SerializeField] private TriggerKind trigger = TriggerKind.Completed;
        [SerializeReference] private EffectConfig effect;

        /// <summary> Построить рантайм-запись эффекта (или null, если эффект не задан) </summary>
        public EffectEntry Build(BuildContext context)
        {
            var built = effect?.Build(context);
            return built != null ? new EffectEntry(trigger, built) : null;
        }
    }
}
