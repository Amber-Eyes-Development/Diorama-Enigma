using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База визуальной реакции: источник — шаг (значение/завершённость и разблокировка)
    /// </summary>
    [RequireComponent(typeof(StepReference))]
    public abstract class InteractableViewBehaviour : MonoBehaviour
    {
        /// <summary> Источник состояния (шаг): значение/завершённость и разблокировка </summary>
        protected SequenceStep StateSource => stateSource;

        private SequenceStep stateSource;

        protected virtual void Awake() => stateSource = GetComponent<StepReference>().Step;

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        /// <summary> Подписаться на источник состояния </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от источника состояния </summary>
        protected abstract void Unsubscribe();
    }
}
