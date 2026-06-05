using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// База визуальной реакции: состояние берётся из значения-шага
    /// </summary>
    public abstract class InteractableViewBehaviour : MonoBehaviour
    {
        /// <summary> Источник состояния (значение-шаг) </summary>
        protected IntValue StateSource => stateSource;

        [Header("Источники"), Space]
        [Tooltip("Состояние-источник (например StateSetPuzzleStep)")]
        [SerializeField] private IntValue stateSource;

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        /// <summary> Подписаться на источник состояния </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от источника состояния </summary>
        protected abstract void Unsubscribe();
    }
}
