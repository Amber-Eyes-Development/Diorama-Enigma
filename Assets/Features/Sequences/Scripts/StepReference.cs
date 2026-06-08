using Extensions.Log;
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
        [SerializeField] private AbstractSequenceStep step;

        /// <summary> Шаг (завершённость/разблокировка как источник для ввода и вью) </summary>
        public AbstractSequenceStep Step => step;

        private void Awake()
        {
            if (step == null) ServiceDebug.LogError($"Шаг {step} не назначен для компонента {name}!");
        }
    }
}
