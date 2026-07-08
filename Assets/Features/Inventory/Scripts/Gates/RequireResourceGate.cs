using System;
using DioramaEnigma.Sequences;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace DioramaEnigma.Inventory
{
    /// <summary> Гейт: требует наличие ресурса не меньше указанного количества </summary>
    [Serializable]
    public sealed class RequireResourceGate : StepGate
    {
        /// <summary> Требуемый ресурс (предмет) </summary>
        public ResourceValue Resource => resource;
        /// <summary> Требуемое количество </summary>
        public int RequiredAmount => requiredAmount;
        /// <summary> Сколько ресурса не хватает до требуемого (0, если хватает) </summary>
        public int Missing => resource == null ? requiredAmount : Mathf.Max(0, requiredAmount - resource.Value);

        [SerializeField] private ResourceValue resource;
        [Tooltip("Требуемое количество ресурса: гейт открыт, когда ресурса не меньше")]
        [FormerlySerializedAs("valueToRemove")]
        [SerializeField] private int requiredAmount = 1;

        /// <inheritdoc/>
        public override bool IsSatisfied() => resource != null && resource.Value >= requiredAmount;

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
