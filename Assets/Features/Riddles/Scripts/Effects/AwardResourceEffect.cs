using Extensions.Events;
using Extensions.Log;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Эффект: устанавливает значение <see cref="BoolValue"/>-ресурса.
    /// Используется для выдачи или изъятия предметов/флагов.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Effects/AwardResourceEffect", fileName = nameof(AwardResourceEffect))]
    public sealed class AwardResourceEffect : PuzzleEffect
    {
        [SerializeField] private BoolValue resource;
        [SerializeField] private bool valueToSet = true;

        /// <inheritdoc/>
        public override void Execute(EventHub hub)
        {
            if (resource == null)
            {
                ServiceDebug.LogWarning<AwardResourceEffect>("resource не назначен");
                return;
            }

            resource.SetValue(valueToSet);
        }
    }
}
