using System;

namespace Extensions.Sequences
{
    /// <summary>
    /// Поведенческий эффект-обёртка над делегатом (код/тесты/награды).
    /// НЕ сериализуется: при восстановлении графа переподвязывается владельцем.
    /// </summary>
    public sealed class CallbackEffect : Effect
    {
        private readonly Action callback;

        /// <summary> Новый эффект-колбэк </summary>
        public CallbackEffect(Action callback) => this.callback = callback;

        /// <inheritdoc/>
        public override void Execute() => callback?.Invoke();
    }
}
