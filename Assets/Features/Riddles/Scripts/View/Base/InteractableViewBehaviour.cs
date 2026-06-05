using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// База визуальной реакции: состояние берётся из значения-шага, наведение — с <see cref="HoverInteractable"/>
    /// </summary>
    public abstract class InteractableViewBehaviour : MonoBehaviour
    {
        /// <summary> Источник состояния (значение-шаг) </summary>
        protected IntValue StateSource => stateSource;
        /// <summary> Источник наведения (опционально) </summary>
        protected HoverInteractable Hover { get; private set; }

        [Header("Источники"), Space]
        [Tooltip("Состояние-источник (например StateSetPuzzleStep)")]
        [SerializeField] private IntValue stateSource;
        [Tooltip("Источник наведения. Пусто — берётся HoverInteractable с этого объекта")]
        [SerializeField] private HoverInteractable hoverSource;

        protected virtual void Awake()
        {
            Hover = hoverSource != null ? hoverSource : GetComponent<HoverInteractable>();
        }

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        /// <summary> Подписаться на источники </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от источников </summary>
        protected abstract void Unsubscribe();
    }
}
