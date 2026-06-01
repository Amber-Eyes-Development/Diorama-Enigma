using System;
using Extensions.Events;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Эффект: блокирует или разблокирует <see cref="InteractableObject"/> по ID.
    /// Используется в <see cref="PuzzleStep.ActivationEffects"/> для разблокировки объекта
    /// при старте шага и в <see cref="PuzzleStep.Effects"/> — для повторной блокировки после завершения.
    /// </summary>
    [Serializable]
    public sealed class SetInteractableLockEffect : PuzzleEffect
    {
        [SerializeField] private InteractableID targetId;
        [SerializeField] private bool isLocked;

        /// <inheritdoc/>
        public override void Execute(EventHub hub)
        {
            if (targetId == null)
            {
                ServiceDebug.LogWarning<SetInteractableLockEffect>("targetId не назначен");
                return;
            }

            hub.Publish(new InteractableLockChangedEvent(targetId.Id, isLocked));
        }
    }
}
