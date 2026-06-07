using System;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие срабатывания реакции
    /// </summary>
    [Serializable]
    public struct ReactionTrigger
    {
        public TriggerKind Kind;

        /// <param name="kind">Тип события</param>
        public ReactionTrigger(TriggerKind kind) => Kind = kind;

        /// <summary> Совпадает ли условие с произошедшим событием </summary>
        /// <param name="fired">Произошедшее событие</param>
        public readonly bool Matches(ReactionTrigger fired) => Kind == fired.Kind;
    }
}
