using DioramaEnigma.Dioramas;
using TMPro;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Рекорд времени блока на кнопке блока в меню: данные берёт из статистики, блок узнаёт от кнопки
    /// </summary>
    /// <remarks> Реализует <see cref="IDioramaButtonBindListener"/> из Dioramas — зависимость односторонняя (Statistics → Dioramas) </remarks>
    public sealed class DioramaBlockRecordLabel : MonoBehaviour, IDioramaButtonBindListener
    {
        [SerializeField] private TMP_Text recordLabel;
        [Tooltip("Корень блока рекорда — скрывается, если рекорда ещё нет (опционально)")]
        [SerializeField] private GameObject recordRoot;

        public void OnBound(DioramaBlock block)
        {
            float best = block != null ? DioramaStatisticsStore.LoadBlockBestTime(block.Id) : 0f;
            bool hasRecord = best > 0f;

            if (recordRoot != null) recordRoot.SetActive(hasRecord);
            if (recordLabel != null) recordLabel.text = hasRecord ? DioramaTimeFormat.Clock(best) : string.Empty;
        }
    }
}
