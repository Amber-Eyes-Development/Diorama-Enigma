using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Гейт: требует, чтобы ресурса было не меньше указанной стоимости
    /// </summary>
    [Serializable]
    public sealed class RequireResourceGate : StepGate
    {
        [SerializeField] private ResourceValue resource;
        [Tooltip("Требуемая (списываемая) стоимость: гейт открыт, когда ресурса не меньше")]
        [SerializeField] private int valueToRemove = 1;

        /// <inheritdoc/>
        public override bool IsSatisfied() => resource != null && resource.Value >= valueToRemove;

        /// <inheritdoc/>
        public override void StartObserving()
        {
            if (resource != null) resource.onValueChanged += OnResourceChanged;
        }

        /// <inheritdoc/>
        public override void StopObserving()
        {
            if (resource != null) resource.onValueChanged -= OnResourceChanged;
        }

        private void OnResourceChanged(int _) => RaiseSatisfactionChanged();
    }
}
