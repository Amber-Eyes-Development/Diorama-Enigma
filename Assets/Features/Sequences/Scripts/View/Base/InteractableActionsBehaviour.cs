using System.Collections;
using System.Collections.Generic;
using Extensions.Coroutines;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База вьюшки со списком действий: каждое действие со своим триггером, обратимостью и задержкой.
    /// Подписывается на шаг, диспатчит действия по событию (с учётом задержки) и приводит их к текущему состоянию при старте.
    /// </summary>
    public abstract class InteractableActionsBehaviour<TAction> : InteractableViewBehaviour
        where TAction : ViewAction, new()
    {
        [SerializeField] private TAction[] actions;

        private Dictionary<TAction, CoroutineTask> delayed;

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            if (actions is { Length: > 0 }) return;

            var action = new TAction();
            action.EnsureTarget(gameObject);
            actions = new[] { action };
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            if (actions == null) return;

            foreach (var action in actions)
                action?.PrepareTarget();
        }

        protected sealed override void Subscribe()
        {
            if (StateSource == null) return;

            StateSource.onCompletionChanged += OnCompletionChanged;
            StateSource.onUnlockChanged += OnUnlockChanged;
            StateSource.onInteractionRejected += OnInteractionRejected;

            ApplyInitial();
        }

        protected sealed override void Unsubscribe()
        {
            CancelDelayed();

            if (StateSource == null) return;

            StateSource.onCompletionChanged -= OnCompletionChanged;
            StateSource.onUnlockChanged -= OnUnlockChanged;
            StateSource.onInteractionRejected -= OnInteractionRejected;
        }

        /// <summary> Выполнить действие </summary>
        /// <param name="action">Запись действия</param>
        /// <param name="reverse">Откат: привести цель к противоположному (исходному) состоянию</param>
        /// <param name="silent">Тихо: без видимого/слышимого проигрыша (восстановление при загрузке)</param>
        protected abstract void Apply(TAction action, bool reverse, bool silent);

        #region Internal

        private void OnCompletionChanged(bool completed) =>
            Dispatch(completed ? TriggerKind.Completed : TriggerKind.NotCompleted, silent: false);

        private void OnUnlockChanged(bool unlocked) =>
            Dispatch(unlocked ? TriggerKind.Unlocked : TriggerKind.Locked, silent: false);

        private void OnInteractionRejected(bool active) =>
            Dispatch(active ? TriggerKind.UnlockedInteractionRejected : TriggerKind.LockedInteractionRejected, silent: false);

        private void Dispatch(TriggerKind fired, bool silent)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action == null) continue;

                if (action.Trigger.Responds(fired))
                    Schedule(action, reverse: false, silent);
                else if (action.Reversible && !action.Trigger.IsChange() && action.Trigger.Opposite() == fired)
                    Schedule(action, reverse: true, silent);
            }
        }

        /// <summary>
        /// Применить действие с учётом его задержки. Тихое применение (восстановление) — всегда мгновенно.
        /// Новое срабатывание отменяет ранее запланированное для того же действия (последний переход выигрывает).
        /// </summary>
        private void Schedule(TAction action, bool reverse, bool silent)
        {
            // Мгновенное (в т.ч. тихое восстановление) применение перебивает ранее запланированное отложенное
            if (silent || action.Delay <= 0f)
            {
                StopDelayed(action);
                Apply(action, reverse, silent);
                return;
            }

            delayed ??= new Dictionary<TAction, CoroutineTask>();
            if (!delayed.TryGetValue(action, out var task))
                delayed[action] = task = new CoroutineTask(this);

            task.Start(DelayedApply(action, reverse, action.Delay)); // CoroutineTask сам гасит предыдущую
        }

        private IEnumerator DelayedApply(TAction action, bool reverse, float delay)
        {
            yield return new WaitForSeconds(delay);
            Apply(action, reverse, silent: false);
        }

        private void StopDelayed(TAction action)
        {
            if (delayed != null && delayed.TryGetValue(action, out var task))
                task.Stop();
        }

        private void CancelDelayed()
        {
            if (delayed == null) return;

            foreach (var task in delayed.Values)
                task.Stop();
        }

        /// <summary> Привести действия к текущему состоянию шага без видимого проигрыша </summary>
        private void ApplyInitial()
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action == null) continue;

                if (action.Trigger.IsChange()) continue;

                if (action.Trigger.IsSatisfiedBy(StateSource.IsCompleted, StateSource.IsUnlocked))
                    Apply(action, reverse: false, silent: true);
                else if (action.Reversible)
                    Apply(action, reverse: true, silent: true);
            }
        }

        #endregion
    }
}
