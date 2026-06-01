using Extensions.Events;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Абстрактный эффект, выполняемый при завершении шага пазла.
    /// Реализации — <see cref="AwardResourceEffect"/>, <see cref="FireEventEffect"/>.
    /// </summary>
    public abstract class PuzzleEffect : ScriptableObject
    {
        /// <summary>Выполнить эффект</summary>
        public abstract void Execute(EventHub hub);
    }
}
