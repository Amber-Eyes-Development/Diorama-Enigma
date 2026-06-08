using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Общая логика шага: гейт, активность, необратимость и отслеживание завершённости/разблокировки
    /// </summary>
    /// <remarks>
    /// Композируется в <see cref="SequenceStep"/> — единственный шаг-значение (булев).
    /// </remarks>
    [Serializable]
    public sealed class StepCompletionTracker
    {
        /// <summary> Изменение признака завершённости </summary>
        public event Action<bool> onCompletionChanged;
        /// <summary> Изменение разблокированности (можно ли менять состояние прямо сейчас) </summary>
        public event Action<bool> onUnlockChanged;

        /// <summary>
        /// Разблокировано ли изменение состояния: активен (группа доступна), гейт открыт
        /// и не сработал латч необратимости (завершённый необратимый шаг неизменен)
        /// </summary>
        public bool IsUnlocked => isActive
            && (gate == null || gate.IsSatisfied())
            && !(irreversible && lastCompleted);

        [Tooltip("Доп. условие, блокирующее изменение состояния (помимо порядка групп). Опционально")]
        [SerializeReference] private StepGate gate;

        private bool isActive;
        private bool irreversible;
        private bool lastCompleted;
        private bool lastUnlocked;

        /// <summary> Инициализировать исходную завершённость/необратимость (вызывать в OnEnable шага) </summary>
        public void Initialize(bool completed, bool irreversible)
        {
            isActive = false;
            this.irreversible = irreversible;
            lastCompleted = completed;
            lastUnlocked = IsUnlocked;
        }

        /// <summary> Активировать/деактивировать изменение состояния. Возвращает true, если активность изменилась </summary>
        public bool SetActive(bool active)
        {
            bool changed = isActive != active;
            if (changed)
            {
                isActive = active;
                ObserveGate(active);
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

        private void ObserveGate(bool observe)
        {
            if (gate == null) return;

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
