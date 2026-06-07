using System.Collections.Generic;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Реакция на состояние шага: диспетчеризация триггеров по биндингам
    /// </summary>
    /// <typeparam name="TBinding">Тип биндинга медиума (визуал / частицы / звук)</typeparam>
    public abstract class InteractableReactionBehaviour<TBinding> : InteractableViewBehaviour
        where TBinding : IReactionBinding
    {
        /// <summary> Биндинги для диспетчеризации (список или один — решает наследник) </summary>
        protected abstract IReadOnlyList<TBinding> Bindings { get; }

        protected sealed override void Subscribe()
        {
            if (StateSource == null) return;

            StateSource.onValueChanged += OnStateChanged;
            StateSource.onUnlockChanged += OnUnlockChanged;

            // Восстанавливаем текущее состояние без эффектов: и стейт, и разблокировку
            Dispatch(new ReactionTrigger(StateKind(StateSource.Value)), silent: true);
            Dispatch(new ReactionTrigger(UnlockKind(StateSource.IsUnlocked)), silent: true);
        }

        protected sealed override void Unsubscribe()
        {
            if (StateSource == null) return;

            StateSource.onValueChanged -= OnStateChanged;
            StateSource.onUnlockChanged -= OnUnlockChanged;
        }

        /// <summary> Применить реакцию биндинга </summary>
        /// <param name="binding">Биндинг с условием и реакцией</param>
        /// <param name="silent">Тихий режим: восстановление состояния без видимых/слышимых эффектов</param>
        protected abstract void Apply(TBinding binding, bool silent);

        #region Internal

        private void OnStateChanged(bool state) =>
            Dispatch(new ReactionTrigger(StateKind(state)), silent: false);

        private void OnUnlockChanged(bool unlocked) =>
            Dispatch(new ReactionTrigger(UnlockKind(unlocked)), silent: false);

        private static TriggerKind StateKind(bool state) => state ? TriggerKind.StateOn : TriggerKind.StateOff;
        private static TriggerKind UnlockKind(bool unlocked) => unlocked ? TriggerKind.Unlocked : TriggerKind.Locked;

        private void Dispatch(ReactionTrigger fired, bool silent)
        {
            var bindings = Bindings;
            if (bindings == null) return;

            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].ReactionTrigger.Matches(fired))
                    Apply(bindings[i], silent);
        }

        #endregion
    }
}
