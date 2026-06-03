using System;

namespace Extensions.Helpers
{
    /// <summary>
    /// IDisposable, выполняющий действие при Dispose
    /// </summary>
    public sealed class ActionDisposable : IDisposable
    {
        private Action disposeAction;
        private bool isDisposed;

        public ActionDisposable(Action disposeAction) => this.disposeAction = disposeAction;

        /// <summary>Выполнить действие отписки</summary>
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
