using Extensions.Identification;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Авторский ассет-определение шага: строит рантайм-<see cref="Step"/> (POCO). Id берётся из ассета.
    /// </summary>
    public abstract class StepDefinition : IdentifiableObject
    {
        /// <summary> Необратимость построенного шага </summary>
        public bool Irreversible => irreversible;

        [Header("Шаг"), Space]
        [Tooltip("Необратимый: после завершения состояние нельзя изменить обратно")]
        [SerializeField] private bool irreversible;

        /// <summary> Построить рантайм-шаг </summary>
        public abstract Step Build(BuildContext context);
    }
}
