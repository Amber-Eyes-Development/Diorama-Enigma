using System;
using Extensions.Events;
using Extensions.Helpers;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие шага паззла, выполняемое при принятии ресурсом заданного значение
    /// </summary>
    [Serializable]
    public sealed class ResourceCondition : PuzzleCondition
    {
        /// <summary> Отслеживаемый ресурс </summary>
        public BoolValue Resource => resource;
        /// <summary>Требуемое значение</summary>
        public bool RequiredValue => requiredValue;

        [SerializeField] private BoolValue resource;
        [SerializeField] private bool requiredValue = true;

        /// <inheritdoc/>
        public override IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null)
        {
            if (Logic.IsNull(resource, nameof(resource))) return null;

            Action<bool> handler = value =>
            {
                if (value == requiredValue)
                    onSatisfied?.Invoke();
            };

            resource.onValueChanged += handler;

            if (resource.Value == requiredValue)
                onSatisfied?.Invoke();

            return new ActionDisposable(() => resource.onValueChanged -= handler);
        }

        /// <inheritdoc/>
        public override bool IsSatisfied(EventHub hub) => resource != null && resource.Value == requiredValue;
    }
}
