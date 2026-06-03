using System;
using Extensions.Events;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Эффект шага паззла, изменяющий состояние интерактивности объекта на сцене
    /// </summary>
    [Serializable]
    public sealed class SetInteractableLockEffect : PuzzleEffect
    {
        [SerializeField] private InteractableID targetId;
        [SerializeField] private bool isLocked;

        /// <inheritdoc/>
        public override void Execute(EventHub hub)
        {
            if (Logic.IsNull(targetId, nameof(targetId))) return;

            hub.Publish(new InteractableLockChangedEvent(targetId.Id, isLocked));
        }
    }
}
