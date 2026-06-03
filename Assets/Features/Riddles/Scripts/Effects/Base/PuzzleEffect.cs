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
        /// <summary> Выполнение эффект </summary>
        public abstract void Execute(EventHub hub);
    }
}
