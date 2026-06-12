using System;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Рантайм-логика шага: активность, гейты, необратимость и отслеживание завершённости/разблокировки.
    /// Конфиг (гейты) хранится на самом шаге и передаётся в <see cref="Initialize"/>.
    /// </summary>
    public sealed class StepCompletionTracker
    {
        /// <summary> Изменение признака завершённости </summary>
        public event Action<bool> onCompletionChanged;
        /// <summary> Изменение разблокированности (можно ли менять состояние прямо сейчас) </summary>
        public event Action<bool> onUnlockChanged;

        /// <summary>
        /// Разблокировано ли изменение состояния: активен (группа доступна), все гейты открыты
        /// и не сработал латч необратимости (завершённый необратимый шаг неизменен)
        /// </summary>
        public bool IsUnlocked => isActive
            && AllGatesSatisfied()
            && !(irreversible && lastCompleted);

        private StepGate[] gates;
        private bool isActive;
        private bool irreversible;
        private bool lastCompleted;
        private bool lastUnlocked;

        /// <summary> Инициализировать исходную завершённость/необратимость (вызывать в OnEnable шага). Гейты приходят при активации </summary>
        public void Initialize(bool completed, bool irreversible)
        {
            gates = null;
            isActive = false;
            this.irreversible = irreversible;
            lastCompleted = completed;
            lastUnlocked = IsUnlocked;
        }

        /// <summary>
        /// Активировать/деактивировать изменение состояния. При активации принимает гейты записи шага
        /// (наблюдение начинается/прекращается здесь). Возвращает true, если активность изменилась.
        /// </summary>
        public bool SetActive(bool active, StepGate[] gates)
        {
            bool changed = isActive != active;
            if (changed)
            {
                if (active)
                {
                    this.gates = gates;
                    isActive = true;
                    ObserveGate(true);
                }
                else
                {
                    ObserveGate(false);
                    isActive = false;
                    this.gates = null;
                }
            }

            NotifyUnlockIfChanged();
            return changed;
        }

        /// <summary> Уведомить подписчиков, если завершённость изменилась (и пересчитать разблокировку) </summary>
        public void NotifyIfChanged(bool completed)
        {
            if (completed != lastCompleted)
            {
                lastCompleted = completed;
                onCompletionChanged?.Invoke(completed);
            }

            // Завершённость влияет на латч необратимости → могла измениться разблокировка
            NotifyUnlockIfChanged();
        }

        #region Internal

        private bool AllGatesSatisfied()
        {
            if (gates == null) return true;

            foreach (var gate in gates)
                if (gate != null && !gate.IsSatisfied())
                    return false;

            return true;
        }

        private void ObserveGate(bool observe)
        {
            if (gates == null) return;

            foreach (var gate in gates)
            {
                if (gate == null) continue;

                if (observe)
                {
                    gate.StartObserving();
                    gate.onSatisfactionChanged += NotifyUnlockIfChanged;
                }
                else
                {
                    gate.onSatisfactionChanged -= NotifyUnlockIfChanged;
                    gate.StopObserving();
                }
            }
        }

        private void NotifyUnlockIfChanged()
        {
            bool unlocked = IsUnlocked;
            if (unlocked == lastUnlocked) return;

            lastUnlocked = unlocked;
            onUnlockChanged?.Invoke(unlocked);
        }

        #endregion
    }
}
