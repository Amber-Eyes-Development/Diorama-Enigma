using System;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Доп. условие, блокирующее изменение состояния шага (помимо порядка групп)
    /// </summary>
    [Serializable]
    public abstract class StepGate
    {
        /// <summary> Изменилась выполненность условия (для уведомления о разблокировке шага) </summary>
        public event Action onSatisfactionChanged;

        /// <summary> Выполнено ли условие разблокировки </summary>
        public abstract bool IsSatisfied();

        /// <summary> Начать наблюдение за зависимостями (вызывается при активации шага) </summary>
        public virtual void StartObserving() { }

        /// <summary> Прекратить наблюдение (вызывается при деактивации шага) </summary>
        public virtual void StopObserving() { }

        /// <summary> Поднять событие изменения выполненности (для наследников) </summary>
        protected void RaiseSatisfactionChanged() => onSatisfactionChanged?.Invoke();
    }
}
