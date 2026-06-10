using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База визуальной реакции: источник — шаг (завершённость и разблокировка)
    /// </summary>
    [RequireComponent(typeof(StepReference))]
    public abstract class InteractableViewBehaviour : MonoBehaviour
    {
        /// <summary> Источник состояния (любой шаг): завершённость, разблокировка, отклонённая попытка </summary>
        protected AbstractSequenceStep StateSource => stateSource;

        private AbstractSequenceStep stateSource;

        protected virtual void Awake() => stateSource = GetComponent<StepReference>().Step;

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        /// <summary> Подписаться на источник состояния </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от источника состояния </summary>
        protected abstract void Unsubscribe();
    }
}
