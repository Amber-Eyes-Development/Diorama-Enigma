using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База компонента ввода: общий источник состояния (шаг <see cref="SequenceStep"/>)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StepReference))]
    public abstract class InteractableInput : MonoBehaviour
    {
        protected SequenceStep State { get; private set; }

        protected virtual void Awake() => State = GetComponent<StepReference>().Step as SequenceStep;
    }
}
