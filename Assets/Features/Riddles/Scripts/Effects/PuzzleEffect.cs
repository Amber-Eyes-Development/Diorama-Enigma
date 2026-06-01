using System;
using Extensions.Events;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Абстрактный эффект, выполняемый при активации/завершении/провале шага пазла.
    /// Хранится инлайн внутри <see cref="PuzzleStep"/> через [SerializeReference].
    /// Реализации — <see cref="AwardResourceEffect"/>, <see cref="FireEventEffect"/>,
    /// <see cref="SetInteractableLockEffect"/>.
    /// </summary>
    [Serializable]
    public abstract class PuzzleEffect
    {
        /// <summary>Выполнить эффект</summary>
        public abstract void Execute(EventHub hub);
    }
}
