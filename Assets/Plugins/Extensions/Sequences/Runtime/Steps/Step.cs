using System.Collections.Generic;
using Extensions.Reactive;

namespace Extensions.Sequences
{
    /// <summary>
    /// Базовый шаг последовательности (POCO, без UnityEngine).
    /// Завершённость — производная (<see cref="EvaluateCompleted"/>); разблокировку ведёт <see cref="StepCompletionTracker"/>.
    /// </summary>
    public abstract class Step
    {
        /// <summary> Изменение признака завершённости </summary>
        public ReadOnlyReactiveProperty<bool> Completed => completed.AsReadOnly();
        /// <summary> Изменение разблокированности (можно ли менять состояние прямо сейчас) </summary>
        public ReadOnlyReactiveProperty<bool> Unlocked => tracker.Unlocked;
        /// <summary> Отклонённая попытка изменить состояние (аргумент: активна ли группа шага) </summary>
        public ReactiveEvent<bool> InteractionRejected => interactionRejected;
        /// <summary> Любое изменение состояния шага (значение/завершённость) — для сейва прогресса </summary>
        public ReactiveEvent StateChanged => stateChanged;

        /// <summary> Идентификатор (для сейва и ссылок между шагами) </summary>
        public string Id => id;
        /// <summary> Завершён ли шаг </summary>
        public bool IsCompleted => completed.Value;
        /// <summary> Можно ли менять состояние шага прямо сейчас </summary>
        public bool IsUnlocked => tracker.IsUnlocked;
        /// <summary> Активна ли группа шага в движке </summary>
        public bool IsActive => tracker.IsActive;
        /// <summary> Необратим ли шаг </summary>
        public bool Irreversible => irreversible;

        private readonly string id;
        private readonly bool irreversible;
        private readonly ReactiveProperty<bool> completed = new(false);
        private readonly ReactiveEvent<bool> interactionRejected = new();
        private readonly ReactiveEvent stateChanged = new();
        private readonly StepCompletionTracker tracker = new();

        /// <summary> Базовый шаг </summary>
        protected Step(string id, bool irreversible)
        {
            this.id = id;
            this.irreversible = irreversible;
        }

        /// <summary> Активировать шаг с условиями (гейтами) записи </summary>
        public void Activate(IReadOnlyList<Gate> gates)
        {
            if (tracker.SetActive(true, gates)) OnActiveChanged(true);
        }

        /// <summary> Деактивировать шаг </summary>
        public void Deactivate()
        {
            if (tracker.SetActive(false, null)) OnActiveChanged(false);
        }

        /// <summary> Сообщить об отклонённой попытке изменить состояние (фидбэк для вьюшек) </summary>
        public void NotifyInteractionRejected() => interactionRejected.Invoke(IsActive);

        /// <summary> Сбросить состояние шага к исходному </summary>
        public abstract void ResetState();

        /// <summary> Вычислить завершённость по текущему состоянию </summary>
        protected abstract bool EvaluateCompleted();

        /// <summary> Хук смены активности (напр. композит активирует/деактивирует детей) </summary>
        protected virtual void OnActiveChanged(bool active) { }

        /// <summary> Зафиксировать исходную завершённость (вызывать последней строкой конструктора наследника) </summary>
        protected void InitializeCompletion()
        {
            bool value = EvaluateCompleted();
            completed.SetValue(value);
            tracker.Initialize(value, irreversible);
        }

        /// <summary> Пересчитать завершённость, оповестить об изменении состояния и (если изменилась) о завершённости </summary>
        protected void RefreshCompletion()
        {
            bool value = EvaluateCompleted();
            bool willChange = value != completed.Value;

            // Сначала StateChanged: сейв успевает зафиксировать состояние до возможной деактивации в reconcile
            stateChanged.Invoke();

            if (willChange)
            {
                completed.SetValue(value);
                tracker.SetCompleted(value);
            }
        }
    }
}
