using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Носитель последнего итога блока — мост между игровой сценой (запись) и окном итогов (чтение)
    /// </summary>
    /// <remarks> Рантайм-значение: пишется при завершении блока, читается окном в той же сцене до перезагрузки </remarks>
    [CreateAssetMenu(menuName = "Dioramas/Results Context", fileName = nameof(DioramaResultsContext))]
    public sealed class DioramaResultsContext : ScriptableObject
    {
        /// <summary> Текущий итог (или null) </summary>
        public DioramaBlockResult Current { get; private set; }

        /// <summary> Задать итог </summary>
        /// <param name="result">Итог прохождения блока</param>
        public void Set(DioramaBlockResult result) => Current = result;

        /// <summary> Сбросить итог </summary>
        public void Clear() => Current = null;
    }
}
