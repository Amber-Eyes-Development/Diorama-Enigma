using System;
using Extensions.Events;
using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Условие выполнено, когда <see cref="BoolValue"/>-ресурс принимает нужное значение.
    /// Проверяется немедленно при активации (ресурс мог быть выдан раньше).
    /// </summary>
    [Serializable]
    public sealed class ResourceCondition : PuzzleCondition
    {
        /// <summary>Отслеживаемый ресурс</summary>
        public BoolValue Resource => resource;

        /// <summary>Значение, при котором условие считается выполненным</summary>
        public bool RequiredValue => requiredValue;

        [SerializeField] private BoolValue resource;
        [SerializeField] private bool requiredValue = true;

        /// <inheritdoc/>
        public override IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null)
        {
            Action<bool> handler = value =>
            {
                if (value == requiredValue)
                    onSatisfied?.Invoke();
            };

            resource.onValueChanged += handler;

            if (resource.Value == requiredValue)
                onSatisfied?.Invoke();

            return new DelegateDisposable(() => resource.onValueChanged -= handler);
        }

        /// <inheritdoc/>
        public override bool IsSatisfied(EventHub hub) => resource != null && resource.Value == requiredValue;
    }
}
