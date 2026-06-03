using System;
using Extensions.Events;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие шага паззла, выполняемое при перетаскивании объекта в заданную зону
    /// </summary>
    [Serializable]
    public sealed class DragCondition : PuzzleCondition
    {
        /// <summary> Перетаскиваемый объект </summary>
        public InteractableID DraggableId => draggableId;
        /// <summary> Зона приземления </summary>
        public InteractableID DropZoneId => dropZoneId;

        [SerializeField] private InteractableID draggableId;
        [SerializeField] private InteractableID dropZoneId;

        /// <inheritdoc/>
        public override IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null)
        {
            if (Logic.IsNull(draggableId, nameof(draggableId)) || Logic.IsNull(dropZoneId, nameof(dropZoneId))) return null;
            
            Action<InteractableDraggedEvent> handler = evt =>
            {
                if (evt.InteractableId == draggableId.Id && evt.DropZoneId == dropZoneId.Id)
                    onSatisfied?.Invoke();
            };

            hub.Subscribe(handler);

            return new DelegateDisposable(() => hub.Unsubscribe(handler));
        }

        /// <inheritdoc/>
        public override bool IsSatisfied(EventHub hub) => false;
    }
}
