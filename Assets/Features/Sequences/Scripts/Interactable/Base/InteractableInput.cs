using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База компонента ввода: общий источник состояния (шаг <see cref="SequenceStep"/>)
    /// </summary>
    /// <remarks> Требует коллайдер (на объекте или его детях) — без него взаимодействие не ловится. </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StepReference))]
    public abstract class InteractableInput : MonoBehaviour
    {
        protected SequenceStep State { get; private set; }

        protected virtual void Awake()
        {
            State = GetComponent<StepReference>().Step as SequenceStep;

            if (GetComponentInChildren<Collider>() == null)
                ServiceDebug.LogWarning(this, "Нет коллайдера (на объекте или его детях) — взаимодействие работать не будет");
        }
    }
}
