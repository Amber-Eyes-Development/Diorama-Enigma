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
        /// <summary> Шаг (ассет, реализующий <see cref="ISequenceStep"/>) </summary>
        public ISequenceStep Step => step as ISequenceStep;
        /// <summary> Индекс параллельной группы </summary>
        public int GroupIndex => groupIndex;
        /// <summary> Доступность группы (свойство группы; на всех её записях должно совпадать) </summary>
        public GroupAvailability Availability => availability;
        /// <summary> Эффекты при активации шага </summary>
        public IReadOnlyList<SequenceStepEffect> ActivationEffects => activationEffects;
        /// <summary> Эффекты при завершении шага </summary>
        public IReadOnlyList<SequenceStepEffect> CompletionEffects => completionEffects;

        [SerializeField] private SequenceStep step;
        [Tooltip("Шаги с одинаковым GroupIndex активируются одновременно. " +
                 "Следующая группа стартует после завершения текущей")]
        [SerializeField] private int groupIndex;
        [Tooltip("Доступность группы: с самого старта (Always) или только после предыдущих групп")]
        [SerializeField] private GroupAvailability availability;
        [Tooltip("Выполняются при активации шага — до ожидания завершения")]
        [SerializeReference] private SequenceStepEffect[] activationEffects = Array.Empty<SequenceStepEffect>();
        [Tooltip("Выполняются при завершении шага")]
        [SerializeReference] private SequenceStepEffect[] completionEffects = Array.Empty<SequenceStepEffect>();
    }
}