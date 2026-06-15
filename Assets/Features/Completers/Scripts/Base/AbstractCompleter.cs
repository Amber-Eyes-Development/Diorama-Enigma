using UnityEngine;
using DioramaEnigma.Sequences;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// База компонента-завершителя: общий источник состояния (шаг <see cref="SequenceStep"/>),
    /// наследник определяет, по какому событию и как менять его значение
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StepReference))]
    public abstract class AbstractCompleter : MonoBehaviour
    {
        protected SequenceStep State { get; private set; }

        protected virtual void Awake() => State = GetComponent<StepReference>().Step as SequenceStep;
    }
}
