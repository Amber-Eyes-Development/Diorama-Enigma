namespace Extensions.Sequences
{
    /// <summary>
    /// Декларативный эффект: принудительно задать значение другому булеву шагу.
    /// Сериализуется (ссылается на цель по id).
    /// </summary>
    public sealed class SetStepEffect : Effect
    {
        /// <summary> Целевой шаг </summary>
        public BoolStep Target => target;
        /// <summary> Устанавливаемое значение </summary>
        public bool Value => value;

        private readonly BoolStep target;
        private readonly bool value;

        /// <summary> Новый эффект задания состояния </summary>
        public SetStepEffect(BoolStep target, bool value = true)
        {
            this.target = target;
            this.value = value;
        }

        /// <inheritdoc/>
        public override void Execute() => target?.ForceValue(value);
    }
}
