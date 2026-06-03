using System;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// IDisposable, вызывающий делегат при Dispose
    /// </summary>
    internal sealed class DelegateDisposable : IDisposable
    {
        private Action onDispose;

        public DelegateDisposable(Action onDispose) => this.onDispose = onDispose;

        public void Dispose()
        {
            onDispose?.Invoke();
            onDispose = null;
        }
    }
}
