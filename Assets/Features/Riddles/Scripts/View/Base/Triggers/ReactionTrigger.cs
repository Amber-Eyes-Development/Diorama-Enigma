using System;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие срабатывания реакции
    /// </summary>
    [Serializable]
    public struct ReactionTrigger
    {
        public TriggerKind Kind;

        [Tooltip("Индекс стейта (только для StateEntered)")]
        public int StateIndex;

        /// <param name="kind">Тип события</param>
        /// <param name="stateIndex">Индекс стейта (только для StateEntered)</param>
        public ReactionTrigger(TriggerKind kind, int stateIndex = 0)
        {
            Kind = kind;
            StateIndex = stateIndex;
        }

        /// <summary> Совпадает ли условие с произошедшим событием </summary>
        /// <param name="fired">Произошедшее событие</param>
        public readonly bool Matches(ReactionTrigger fired) =>
            Kind == fired.Kind && (Kind != TriggerKind.StateEntered || StateIndex == fired.StateIndex);
    }
}
