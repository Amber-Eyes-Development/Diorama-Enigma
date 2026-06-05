using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Реакция на состояние шага и наведение: диспетчеризация триггеров по списку биндингов
    /// </summary>
    /// <typeparam name="TBinding">Тип биндинга медиума (визуал / частицы / звук)</typeparam>
    public abstract class InteractableReactionBehaviour<TBinding> : InteractableViewBehaviour
        where TBinding : IReactionBinding
    {
        [SerializeField] private TBinding[] bindings;

        protected sealed override void Subscribe()
        {
            if (Hover != null)
                Hover.onHoverChanged += OnHoverChanged;

            if (StateSource != null)
            {
                StateSource.onValueChanged += OnStateChanged;

                // Восстановление текущего состояния без эффектов
                if (StateSource.Value != 0)
                    Dispatch(new ReactionTrigger(TriggerKind.StateEntered, StateSource.Value), silent: true);
            }
        }

        protected sealed override void Unsubscribe()
        {
            if (Hover != null)
                Hover.onHoverChanged -= OnHoverChanged;

            if (StateSource != null)
                StateSource.onValueChanged -= OnStateChanged;
        }

        /// <summary> Применить реакцию биндинга </summary>
        /// <param name="binding">Биндинг с условием и реакцией</param>
        /// <param name="silent">Тихий режим: восстановление состояния без видимых/слышимых эффектов</param>
        protected abstract void Apply(TBinding binding, bool silent);

        #region Internal

        private void OnHoverChanged(bool isHovered) =>
            Dispatch(new ReactionTrigger(isHovered ? TriggerKind.HoverEnter : TriggerKind.HoverExit), silent: false);

        private void OnStateChanged(int stateIndex) =>
            Dispatch(new ReactionTrigger(TriggerKind.StateEntered, stateIndex), silent: false);

        private void Dispatch(ReactionTrigger fired, bool silent)
        {
            if (bindings == null) return;

            foreach (var binding in bindings)
                if (binding.ReactionTrigger.Matches(fired))
                    Apply(binding, silent);
        }

        #endregion
    }
}
