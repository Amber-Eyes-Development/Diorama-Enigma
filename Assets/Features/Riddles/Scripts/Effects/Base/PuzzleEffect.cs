using System;
using Extensions.Events;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Абстракция эффекта шага пазла
    /// </summary>
    [Serializable]
    public abstract class PuzzleEffect
    {
        /// <summary> Выполнить эффект </summary>
        public abstract void Execute(EventHub hub);
    }
}
