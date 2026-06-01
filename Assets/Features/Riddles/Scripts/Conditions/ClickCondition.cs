using System;
using Extensions.Events;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие выполнено, когда <see cref="InteractableObject"/> с заданным ID достигает нужного стейта.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Conditions/ClickCondition", fileName = nameof(ClickCondition))]
    public sealed class ClickCondition : PuzzleCondition
    {
        /// <summary>Целевой объект</summary>
        public InteractableID TargetId => targetId;

        /// <summary>Индекс стейта, при достижении которого условие выполнено</summary>
        public int RequiredStateIndex => requiredStateIndex;

        [SerializeField] private InteractableID targetId;
        [SerializeField] private int requiredStateIndex = 1;

        /// <inheritdoc/>
        public override IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null)
        {
            Action<InteractableClickedEvent> handler = evt =>
            {
                if (evt.InteractableId == targetId.Id && evt.NewStateIndex == requiredStateIndex)
                    onSatisfied?.Invoke();
            };

            hub.Subscribe(handler);

            return new DelegateDisposable(() => hub.Unsubscribe(handler));
        }

        /// <inheritdoc/>
        public override bool IsSatisfied(EventHub hub)
        {
            return hub.TryGetLastEvent<InteractableClickedEvent>(out var evt)
                   && evt.InteractableId == targetId.Id
                   && evt.NewStateIndex == requiredStateIndex;
        }
    }
}
