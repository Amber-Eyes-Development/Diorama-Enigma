using System;
using Extensions.Events;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Базовое условие шага пазла
    /// </summary>
    [Serializable]
    public abstract class PuzzleCondition
    {
        /// <summary> Активировать условие </summary>
        /// <returns> Объект отписки </returns>
        public abstract IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null);
        /// <summary> Выполнено ли условие </summary>
        public abstract bool IsSatisfied(EventHub hub);
    }
}
