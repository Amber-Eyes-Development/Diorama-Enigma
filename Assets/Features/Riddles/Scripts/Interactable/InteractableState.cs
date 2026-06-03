using System;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Состояние интерактивного объекта
    /// </summary>
    public sealed class InteractableState
    {
        /// <summary> Смена состояния </summary>
        public event Action<int> onStateChanged;
        /// <summary> Текущий индекс состояния </summary>
        public int Current { get; private set; }

        private readonly int stateCount;

        public InteractableState(int stateCount)
        {
            if (stateCount < 2)
                throw new ArgumentOutOfRangeException(nameof(stateCount), "Количество состояний stateCount должно быть = хотя бы 2");

            this.stateCount = stateCount;
        }

        /// <summary> Перейти к следующему состоянию с зацикливанием </summary>
        public void CycleNext()
        {
            Current = (Current + 1) % stateCount;
            onStateChanged?.Invoke(Current);
        }

        /// <summary> Сбросить состояние к начальному </summary>
        public void Reset()
        {
            Current = 0;
            onStateChanged?.Invoke(Current);
        }

        /// <summary> Установить состояние без уведомления подписчиков </summary>
        public void SetSilent(int index)
        {
            if (index < 0 || index >= stateCount) return;

            Current = index;
        }
    }
}
