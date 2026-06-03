using System;
using Extensions.Events;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие шага паззла, выполняемое при достижении объектом заданного состояния
    /// </summary>
    [Serializable]
    public sealed class ClickCondition : PuzzleCondition
    {
        /// <summary> Целевой объект </summary>
        public InteractableID TargetId => targetId;
        /// <summary> Требуемый индекс состояния </summary>
        public int RequiredStateIndex => requiredStateIndex;

        [SerializeField] private InteractableID targetId;
        [Min(0)]
        [SerializeField] private int requiredStateIndex = 1;

        /// <inheritdoc/>
        public override IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null)
        {
            if (Logic.IsNull(targetId, nameof(targetId))) return null;

            Action<InteractableClickedEvent> handler = evt =>
            {
                if (evt.InteractableId == targetId.Id && evt.NewStateIndex == requiredStateIndex)
                    onSatisfied?.Invoke();
            };

            hub.Subscribe(handler);

            return new ActionDisposable(() => hub.Unsubscribe(handler));
        }

        /// <inheritdoc/>
        public override bool IsSatisfied(EventHub hub)
        {
            return targetId != null
                   && hub.TryGetLastEvent<InteractableClickedEvent>(out var evt)
                   && evt.InteractableId == targetId.Id
                   && evt.NewStateIndex == requiredStateIndex;
        }
    }
}
