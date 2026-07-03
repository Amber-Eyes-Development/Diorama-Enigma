using Extensions.Reactive;

namespace Extensions.Sequences
{
    /// <summary>
    /// Доп. условие, блокирующее изменение состояния шага (помимо порядка групп)
    /// </summary>
    public abstract class Gate
    {
        /// <summary> Изменилась выполненность условия (для пересчёта разблокировки шага) </summary>
        public ReactiveEvent SatisfactionChanged { get; } = new();

        /// <summary> Выполнено ли условие разблокировки </summary>
        public abstract bool IsSatisfied();

        /// <summary> Начать наблюдение за зависимостями (при активации шага) </summary>
        public virtual void StartObserving() { }

        /// <summary> Прекратить наблюдение (при деактивации шага) </summary>
        public virtual void StopObserving() { }

        /// <summary> Поднять событие изменения выполненности (для наследников) </summary>
        protected void RaiseSatisfactionChanged() => SatisfactionChanged.Invoke();
    }
}
