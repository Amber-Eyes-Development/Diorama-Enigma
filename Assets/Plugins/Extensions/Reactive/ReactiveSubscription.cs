using System;

namespace Extensions.Reactive
{
    /// <summary>
    /// Подписка на реактивный источник
    /// </summary>
    /// <remarks>
    /// Упрощает отписку: нет необходимости явно указывать слушателей, так же отписываются лямбды
    /// </remarks>
    public sealed class ReactiveSubscription : IDisposable
    {
        private Action disposeAction;
        private bool isDisposed;

        internal ReactiveSubscription(Action disposeAction) => this.disposeAction = disposeAction;

        /// <summary>
        /// Отписка от источника
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;

            Action action = disposeAction;
            disposeAction = null;

            action?.Invoke();
        }
    }
}