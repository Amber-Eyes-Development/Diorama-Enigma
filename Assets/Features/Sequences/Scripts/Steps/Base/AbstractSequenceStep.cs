using System;
using System.Collections.Generic;
using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Базовый шаг последовательности
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
        /// <summary> Отклонённая попытка изменить состояние шага </summary>
        /// <remarks>
        /// Аргумент: true — группа активна, но изменению мешает другое условие (гейт/кулдаун/латч необратимости);
        /// false — группа неактивна (шаг заблокирован) 
        /// </remarks>
        public event Action<bool> onInteractionRejected;

        /// <summary> Завершён ли шаг </summary>
        public abstract bool IsCompleted { get; }
        /// <summary> Можно ли менять состояние шага прямо сейчас (активен, гейт открыт, не залочен) </summary>
        public bool IsUnlocked => tracker.IsUnlocked;
        /// <summary> Активна ли группа шага в раннере (безотносительно гейтов/латча необратимости) </summary>
        public bool IsActive => tracker.IsActive;
        /// <summary> Активные гейты шага (условия доступа; null, если группа неактивна) </summary>
        public IReadOnlyList<StepGate> ActiveGates => tracker.Gates;
        /// <summary> Является ли шаг необратимым </summary>
        public bool Irreversible => irreversible;

        [Header("Шаг"), Space]
        [Tooltip("Необратимый: после завершения состояние нельзя изменить обратно")]
        [SerializeField] private bool irreversible;

        private readonly StepCompletionTracker tracker = new();

        /// <summary> Активировать/деактивировать шаг </summary>
        public void SetActive(bool active, StepGate[] gates = null)
        {
            if (tracker.SetActive(active, gates))
                OnActiveChanged(active);
        }

        /// <summary> Сообщить об отклонённой попытке изменить состояние (вызывает ввод при недоступном шаге) </summary>
        public void NotifyInteractionRejected() => onInteractionRejected?.Invoke(IsActive);

        /// <summary> Сбросить состояние шага к исходному </summary>
        public abstract void ResetState();

        protected virtual void OnEnable() => tracker.Initialize(IsCompleted, irreversible);

        /// <summary> Хук смены активности для наследников (напр. композит активирует детей) </summary>
        protected virtual void OnActiveChanged(bool active) { }

        /// <summary> Пересчитать завершённость и уведомить подписчиков, если изменилась </summary>
        protected void NotifyCompletionChanged() => tracker.NotifyIfChanged(IsCompleted);
    }
}
