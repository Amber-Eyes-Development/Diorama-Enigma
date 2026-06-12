using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Запись шага: ссылка на ассет-шаг, индекс параллельной группы и эффекты оркестрации
    /// </summary>
    [Serializable]
    public sealed class StepEntry
    {
        /// <summary> Шаг последовательности </summary>
        public AbstractSequenceStep Step => step;
        /// <summary> Индекс параллельной группы </summary>
        public int GroupIndex => groupIndex;
        /// <summary> Доступность группы (свойство группы; на всех её записях должно совпадать) </summary>
        public GroupAvailability Availability => availability;
        /// <summary>
        /// Остаётся ли группа интерактивной после завершения (свойство группы; на всех её записях совпадает)
        /// </summary>
        public bool InteractableAfterCompletion => interactableAfterCompletion;
        /// <summary> Эффекты шага: каждый со своим триггером (событием шага) </summary>
        public IReadOnlyList<EffectEntry> Effects => effects;
        /// <summary> Условия доступа к изменению состояния шага (помимо порядка групп): все должны быть выполнены </summary>
        public StepGate[] Gates => gates;

        [SerializeField] private AbstractSequenceStep step;
        [Tooltip("Шаги с одинаковым GroupIndex активируются одновременно. " +
                 "Следующая группа стартует после завершения текущей")]
        [SerializeField] private int groupIndex;
        [Tooltip("Доступность группы: с самого старта (Always) или только после предыдущих групп")]
        [SerializeField] private GroupAvailability availability;
        [Tooltip("Если включено — шаги группы остаются доступны для изменения после её завершения " +
                 "(прогресс при этом не откатывается)")]
        [SerializeField] private bool interactableAfterCompletion;
        [Tooltip("Эффекты шага: каждый со своим триггером (событием шага)")]
        [SerializeField] private EffectEntry[] effects = Array.Empty<EffectEntry>();
        [Tooltip("Условия доступа к изменению состояния шага (помимо порядка групп): все должны быть выполнены")]
        [SerializeReference] private StepGate[] gates = Array.Empty<StepGate>();
    }
}