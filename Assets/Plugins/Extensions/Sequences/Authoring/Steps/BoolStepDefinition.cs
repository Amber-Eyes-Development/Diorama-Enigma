using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Определение булева шага
    /// </summary>
    [CreateAssetMenu(menuName = "Extensions/Sequences/Steps/Bool Step", fileName = nameof(BoolStepDefinition))]
    public sealed class BoolStepDefinition : StepDefinition
    {
        [Header("Значение"), Space]
        [Tooltip("Значение по умолчанию")]
        [SerializeField] private bool defaultValue;
        [Tooltip("Значение, при котором шаг считается завершённым")]
        [SerializeField] private bool completionState = true;

        /// <inheritdoc/>
        public override Step Build(BuildContext context) =>
            new BoolStep(Id, completionState, defaultValue, Irreversible);
    }
}
