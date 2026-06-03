using System;
using Extensions.Events;
using Extensions.Helpers;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Эффект шага паззла, публикующий игровое событие по идентификатору
    /// </summary>
    [Serializable]
    public sealed class FireEventEffect : PuzzleEffect
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
