namespace Extensions.Sequences
{
    /// <summary>
    /// Абстракция эффекта шага (реакция на событие-триггер)
    /// </summary>
    public abstract class Effect
    {
        /// <summary> Выполнить эффект </summary>
        public abstract void Execute();
    }
}
