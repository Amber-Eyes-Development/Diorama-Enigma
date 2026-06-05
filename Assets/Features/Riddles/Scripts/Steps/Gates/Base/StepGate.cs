using System;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Доп. условие, блокирующее изменение состояния шага (помимо порядка групп)
    /// </summary>
    [Serializable]
    public abstract class StepGate
    {
        /// <summary> Выполнено ли условие разблокировки </summary>
        public abstract bool IsSatisfied();
    }
}
