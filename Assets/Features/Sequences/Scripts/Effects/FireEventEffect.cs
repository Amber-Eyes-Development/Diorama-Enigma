using System;
using Extensions.Events;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Эффект шага последовательности, публикующий игровое событие по идентификатору
    /// </summary>
    [Serializable]
    public sealed class FireEventEffect : SequenceStepEffect
    {
        [SerializeField] private GameEventID gameEventId;

        /// <inheritdoc/>
        public override void Execute(EventHub hub)
        {
            if (Logic.IsNull(gameEventId, nameof(gameEventId))) return;

            hub.Publish(new GameEventFiredEvent(gameEventId.Id));
        }
    }
}
