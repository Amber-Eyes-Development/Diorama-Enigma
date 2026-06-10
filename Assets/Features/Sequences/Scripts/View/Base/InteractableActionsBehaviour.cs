using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База вьюшки со списком действий: каждое действие со своим триггером и обратимостью.
    /// Подписывается на шаг, диспатчит действия по событию и приводит их к текущему состоянию при старте.
    /// </summary>
    public abstract class InteractableActionsBehaviour<TAction> : InteractableViewBehaviour
        where TAction : ViewAction, new()
    {
        [SerializeField] private TAction[] actions;

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

            // Вьюшка — единоличный распорядитель проигрывания: гасим авто-старт целей (до их собственного Awake, см. порядок выполнения)
            foreach (var action in actions)
                action?.PrepareTarget();
        }

        protected sealed override void Subscribe()
        {
            if (StateSource == null) return;

            StateSource.onCompletionChanged += OnCompletionChanged;
            StateSource.onUnlockChanged += OnUnlockChanged;

            ApplyInitial();
        }

        protected sealed override void Unsubscribe()
        {
            if (StateSource == null) return;

            StateSource.onCompletionChanged -= OnCompletionChanged;
            StateSource.onUnlockChanged -= OnUnlockChanged;
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

        private void Dispatch(TriggerKind fired, bool silent)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action == null) continue;

                if (action.Trigger.Responds(fired))
                    Apply(action, reverse: false, silent);
                else if (action.Reversible && !action.Trigger.IsChange() && action.Trigger.Opposite() == fired)
                    Apply(action, reverse: true, silent);
            }
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
