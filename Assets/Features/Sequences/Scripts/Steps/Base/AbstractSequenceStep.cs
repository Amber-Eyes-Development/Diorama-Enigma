using System;
using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Базовый шаг последовательности: общий жизненный цикл — метка, активация/гейт/разблокировка
    /// (через <see cref="StepCompletionTracker"/>), завершённость и сброс.
    /// Наследник определяет только то, как вычисляется <see cref="IsCompleted"/>.
    /// </summary>
    public abstract class AbstractSequenceStep : IdentifiableObject
    {
        /// <summary> Изменение признака завершённости </summary>
        public event Action<bool> onCompletionChanged
        {
            add => tracker.onCompletionChanged += value;
            remove => tracker.onCompletionChanged -= value;
        }
        /// <summary> Изменение разблокированности (можно ли менять состояние прямо сейчас) </summary>
        public event Action<bool> onUnlockChanged
        {
            add => tracker.onUnlockChanged += value;
            remove => tracker.onUnlockChanged -= value;
        }

        /// <summary> Метка шага </summary>
        public string StepLabel => stepLabel;
        /// <summary> Завершён ли шаг </summary>
        public abstract bool IsCompleted { get; }
        /// <summary> Можно ли менять состояние шага прямо сейчас (активен, гейт открыт, не залочен) </summary>
        public bool IsUnlocked => tracker.IsUnlocked;

        [Header("Шаг"), Space]
        [SerializeField] private string stepLabel;
        [Tooltip("Необратимый: после завершения состояние нельзя изменить обратно")]
        [SerializeField] private bool irreversible;
        [SerializeField] private StepCompletionTracker tracker = new();

        /// <summary> Активировать/деактивировать шаг (раннер открывает изменение состояния) </summary>
        public void SetActive(bool active)
        {
            if (tracker.SetActive(active))
                OnActiveChanged(active);
        }

        /// <summary> Сбросить состояние шага к исходному </summary>
        public abstract void ResetState();

        protected virtual void OnEnable() => tracker.Initialize(IsCompleted, irreversible);

        /// <summary> Хук смены активности для наследников (напр. композит активирует детей) </summary>
        protected virtual void OnActiveChanged(bool active) { }

        /// <summary> Пересчитать завершённость и уведомить подписчиков, если изменилась </summary>
        protected void NotifyCompletionChanged() => tracker.NotifyIfChanged(IsCompleted);
    }
}
