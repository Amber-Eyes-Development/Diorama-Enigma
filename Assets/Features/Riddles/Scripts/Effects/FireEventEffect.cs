using Extensions.Events;
using Extensions.Identification;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Эффект: публикует <see cref="GameEventFiredEvent"/> в хаб событий.
    /// Используется для уведомления других систем о завершении шага.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Effects/FireEventEffect", fileName = nameof(FireEventEffect))]
    public sealed class FireEventEffect : PuzzleEffect
    {
        [SerializeField] private ID gameEventId;

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
