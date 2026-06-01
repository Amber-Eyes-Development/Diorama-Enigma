using System;
using Extensions.Events;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Эффект: публикует <see cref="GameEventFiredEvent"/> в хаб событий.
    /// Используется для уведомления других систем о завершении шага.
    /// </summary>
    [Serializable]
    public sealed class FireEventEffect : PuzzleEffect
    {
        [SerializeField] private GameEventID gameEventId;

        /// <inheritdoc/>
        public override void Execute(EventHub hub)
        {
            if (gameEventId == null)
            {
                ServiceDebug.LogWarning<FireEventEffect>("gameEventId не назначен");
                return;
            }

            hub.Publish(new GameEventFiredEvent(gameEventId.Id));
        }
    }
}
