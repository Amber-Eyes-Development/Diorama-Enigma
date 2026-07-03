using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Определение шага-счётчика
    /// </summary>
    [CreateAssetMenu(menuName = "Extensions/Sequences/Steps/Counter Step", fileName = nameof(CounterStepDefinition))]
    public sealed class CounterStepDefinition : StepDefinition
    {
        [Header("Счётчик"), Space]
        [Tooltip("Порог, при котором шаг завершён")]
        [Min(0)]
        [SerializeField] private int target = 1;
        [Tooltip("Значение по умолчанию")]
        [Min(0)]
        [SerializeField] private int defaultCount;

        /// <inheritdoc/>
        public override Step Build(BuildContext context) =>
            new CounterStep(Id, target, defaultCount, Irreversible);
    }
}
