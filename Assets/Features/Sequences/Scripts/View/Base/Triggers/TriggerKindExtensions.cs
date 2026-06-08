namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Хелперы сопоставления триггеров с событиями шага
    /// </summary>
    public static class TriggerKindExtensions
    {
        /// <summary> Триггер «на любое изменение» (без конкретного состояния) </summary>
        public static bool IsChange(this TriggerKind trigger) =>
            trigger is TriggerKind.CompletionStateChange or TriggerKind.LockStateChange;

        /// <summary>
        /// Реагирует ли триггер на конкретное событие (<paramref name="fired"/> — один из четырёх конкретных)
        /// </summary>
        public static bool Responds(this TriggerKind trigger, TriggerKind fired) =>
            trigger == fired
            || (trigger == TriggerKind.CompletionStateChange &&
                fired is TriggerKind.Completed or TriggerKind.NotCompleted)
            || (trigger == TriggerKind.LockStateChange &&
                fired is TriggerKind.Unlocked or TriggerKind.Locked);

        /// <summary> Противоположное событие (для отката направленных триггеров); у «изменений» — само себя </summary>
        public static TriggerKind Opposite(this TriggerKind trigger) => trigger switch
        {
            TriggerKind.Completed => TriggerKind.NotCompleted,
            TriggerKind.NotCompleted => TriggerKind.Completed,
            TriggerKind.Unlocked => TriggerKind.Locked,
            TriggerKind.Locked => TriggerKind.Unlocked,
            _ => trigger,
        };

        /// <summary>
        /// Выполнено ли стационарное условие триггера по текущему состоянию шага.
        /// «Изменения» стационарного состояния не имеют — всегда false.
        /// </summary>
        public static bool IsSatisfiedBy(this TriggerKind trigger, bool completed, bool unlocked) => trigger switch
        {
            TriggerKind.Completed => completed,
            TriggerKind.NotCompleted => !completed,
            TriggerKind.Unlocked => unlocked,
            TriggerKind.Locked => !unlocked,
            _ => false,
        };
    }
}
