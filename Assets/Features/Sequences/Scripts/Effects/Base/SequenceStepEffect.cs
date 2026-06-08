using System;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Абстракция эффекта шага последовательности
    /// </summary>
    [Serializable]
    public abstract class SequenceStepEffect
    {
        /// <summary> Выполнить эффект </summary>
        public abstract void Execute();
    }
}
