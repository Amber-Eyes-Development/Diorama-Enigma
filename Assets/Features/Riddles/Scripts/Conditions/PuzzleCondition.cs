using System;
using Extensions.Events;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Абстрактное условие пазла. Хранится инлайн внутри <see cref="PuzzleStep"/>
    /// через [SerializeReference]. Реализации — <see cref="ClickCondition"/>,
    /// <see cref="DragCondition"/>, <see cref="ResourceCondition"/>, <see cref="DelayedCondition"/>.
    /// </summary>
    [Serializable]
    public abstract class PuzzleCondition
    {
        /// <summary>
        /// Активировать условие: подписаться на события хаба.
        /// Вызывает <paramref name="onSatisfied"/> при выполнении условия,
        /// <paramref name="onFailed"/> при провале (если условие его поддерживает).
        /// Возвращённый IDisposable снимает все подписки при вызове Dispose.
        /// </summary>
        public abstract IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null);

        /// <summary>
        /// Проверить выполнение условия без подписки (например, по replay-событиям).
        /// Используется для восстановления состояния.
        /// </summary>
        public abstract bool IsSatisfied(EventHub hub);
    }
}
