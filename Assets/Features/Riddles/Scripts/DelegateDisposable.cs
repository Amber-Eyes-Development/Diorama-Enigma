using System;

namespace DioramaEnigma.Riddles
{
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
