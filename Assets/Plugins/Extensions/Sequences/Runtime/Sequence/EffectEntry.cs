namespace Extensions.Sequences
{
    /// <summary>
    /// Запись эффекта: триггер (событие шага) + сам эффект
    /// </summary>
    public sealed class EffectEntry
    {
        /// <summary> Событие шага, на которое реагирует эффект </summary>
        public TriggerKind Trigger => trigger;
        /// <summary> Эффект </summary>
        public Effect Effect => effect;

        private readonly TriggerKind trigger;
        private readonly Effect effect;

        /// <summary> Новая запись эффекта </summary>
        public EffectEntry(TriggerKind trigger, Effect effect)
        {
            this.trigger = trigger;
            this.effect = effect;
        }
    }
}
