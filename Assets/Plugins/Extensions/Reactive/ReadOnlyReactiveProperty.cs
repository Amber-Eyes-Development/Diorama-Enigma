using System;

namespace Extensions.Reactive
{
    /// <summary>
    /// Read-only реактивное свойство
    /// </summary>
    public sealed class ReadOnlyReactiveProperty<T>
    {
        private readonly ReactiveProperty<T> source;

        internal ReadOnlyReactiveProperty(ReactiveProperty<T> source) => this.source = source;

        /// <summary>
        /// Текущее значение
        /// </summary>
        public T Value => source.Value;

        /// <summary>
        /// Подписка на изменение значения
        /// </summary>
        /// <param name="callback">Событие</param>
        /// <param name="notifyImmediately">Оповестить после подписки</param>
        /// <returns>Подписка на реактивный источник</returns>
        public ReactiveSubscription Subscribe(Action<T> callback, bool notifyImmediately = false)
        {
            return source.Subscribe(callback, notifyImmediately);
        }

        /// <summary>
        /// Подписка с получением предыдущего и нового значения (diff)
        /// </summary>
        /// <param name="callback">Событие</param>
        /// <param name="notifyImmediately">Оповестить после подписки</param>
        /// <returns>Подписка на реактивный источник</returns>
        public ReactiveSubscription Subscribe(Action<T, T> callback, bool notifyImmediately = false)
        {
            return source.Subscribe(callback, notifyImmediately);
        }
    }
}