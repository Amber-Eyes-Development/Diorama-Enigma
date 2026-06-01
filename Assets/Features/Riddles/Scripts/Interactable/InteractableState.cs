using System;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Runtime-состояние интерактивного объекта: текущий индекс стейта и логика цикличного переключения
    /// </summary>
    public sealed class InteractableState
    {
        /// <summary>Событие смены стейта; передаёт новый индекс</summary>
        public event Action<int> onStateChanged;

        /// <summary>Текущий индекс стейта (0 — начальный)</summary>
        public int Current { get; private set; }

        private readonly int stateCount;

        public InteractableState(int stateCount)
        {
            if (stateCount < 2)
                throw new ArgumentOutOfRangeException(nameof(stateCount), "stateCount must be at least 2");

            this.stateCount = stateCount;
        }

        /// <summary>Перейти к следующему стейту (с зацикливанием)</summary>
        public void CycleNext()
        {
            Current = (Current + 1) % stateCount;
            onStateChanged?.Invoke(Current);
        }

        /// <summary>Сбросить стейт к начальному (0)</summary>
        public void Reset()
        {
            Current = 0;
            onStateChanged?.Invoke(Current);
        }

        /// <summary>
        /// Установить стейт без уведомления подписчиков.
        /// Используется при восстановлении состояния из сохранения.
        /// </summary>
        public void SetSilent(int index)
        {
            if (index < 0 || index >= stateCount) return;

            Current = index;
        }
    }
}
