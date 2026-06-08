using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База реакции вью: один триггер → одна реакция. Нужно ещё — добавь второй компонент.
    /// </summary>
    public abstract class InteractableReactionBehaviour : InteractableViewBehaviour
    {
        [Tooltip("Событие шага, на которое реагирует этот компонент")]
        [SerializeField] private TriggerKind trigger;

        protected sealed override void Subscribe()
        {
            if (StateSource == null) return;

            StateSource.onCompletionChanged += OnCompletionChanged;
            StateSource.onUnlockChanged += OnUnlockChanged;

            TryReact(CompletionKind(StateSource.IsCompleted), silent: true);
            TryReact(UnlockKind(StateSource.IsUnlocked), silent: true);
        }

        protected sealed override void Unsubscribe()
        {
            if (StateSource == null) return;

            StateSource.onCompletionChanged -= OnCompletionChanged;
            StateSource.onUnlockChanged -= OnUnlockChanged;
        }

        /// <summary> Применить реакцию </summary>
        /// <param name="silent">Тихий режим: восстановление состояния без видимых/слышимых эффектов</param>
        protected abstract void React(bool silent);

        #region Internal

        private void OnCompletionChanged(bool completed) => TryReact(CompletionKind(completed), silent: false);

        private void OnUnlockChanged(bool unlocked) => TryReact(UnlockKind(unlocked), silent: false);

        private void TryReact(TriggerKind fired, bool silent)
        {
            if (trigger == fired) React(silent);
        }

        private static TriggerKind CompletionKind(bool completed) => completed ? TriggerKind.Completed : TriggerKind.NotCompleted;
        private static TriggerKind UnlockKind(bool unlocked) => unlocked ? TriggerKind.Unlocked : TriggerKind.Locked;

        #endregion
    }
}
