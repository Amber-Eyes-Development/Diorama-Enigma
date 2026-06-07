using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Единый держатель ссылки на шаг для объекта сцены.
    /// </summary>
    /// <remarks>
    /// Скрипты ввода и вью берут шаг отсюда (через <c>RequireComponent</c> + <c>GetComponent</c> в Awake),
    /// а не хранят ссылку на ассет каждый сам. Так ссылка на шаг задаётся на объекте один раз.
    /// Тип поля — <see cref="PuzzleStep"/>: инспектор принимает только шаги-ассеты, а не любое значение.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class StepReference : MonoBehaviour
    {
        [Tooltip("Шаг-ассет, общий источник состояния для ввода и вью на этом объекте")]
        [SerializeField] private PuzzleStep step;

        /// <summary> Шаг (значение/завершённость для ввода, разблокировка для вью) </summary>
        public PuzzleStep Step => step;
    }
}
