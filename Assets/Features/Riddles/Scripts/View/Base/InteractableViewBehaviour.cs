using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// База визуальной реакции: источник — шаг (значение/завершённость и разблокировка)
    /// </summary>
    /// <remarks>Ссылку на шаг берёт из <see cref="StepReference"/> на том же объекте.</remarks>
    [RequireComponent(typeof(StepReference))]
    public abstract class InteractableViewBehaviour : MonoBehaviour
    {
        /// <summary> Источник состояния (шаг): значение/завершённость и разблокировка </summary>
        protected PuzzleStep StateSource => stateSource;

        private PuzzleStep stateSource;

        protected virtual void Awake() => stateSource = GetComponent<StepReference>().Step;

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        /// <summary> Подписаться на источник состояния </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от источника состояния </summary>
        protected abstract void Unsubscribe();
    }
}
