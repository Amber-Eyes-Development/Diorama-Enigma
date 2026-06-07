using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Единый держатель ссылки на шаг для объекта сцены
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StepReference : MonoBehaviour
    {
        [Tooltip("Шаг-ассет, общий источник состояния для ввода и вью на этом объекте")]
        [SerializeField] private SequenceStep step;

        /// <summary> Шаг (значение/завершённость для ввода, разблокировка для вью) </summary>
        public SequenceStep Step => step;
    }
}
