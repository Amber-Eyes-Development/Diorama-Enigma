using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Окно итогов блока: суммарное время, рекорд, количество диорам и разбивка времени по диорамам
    /// </summary>
    public sealed class DioramaBlockResultView : MonoBehaviour
    {
        [Tooltip("Носитель итога (ассет-мост)")]
        [SerializeField] private DioramaResultsContext results;

        [Header("Сводка")]
        [SerializeField] private TMP_Text blockTitle;
        [SerializeField] private TMP_Text totalTime;
        [SerializeField] private TMP_Text dioramaCount;
        [Tooltip("Текст рекорда (лучшего времени)")]
        [SerializeField] private TMP_Text recordTime;
        [Tooltip("Бейдж нового рекорда (опционально)")]
        [SerializeField] private GameObject newRecordBadge;

        [Header("Разбивка по диорамам")]
        [Tooltip("Контейнер строк")]
        [SerializeField] private Transform rowContainer;
        [Tooltip("Префаб строки")]
        [SerializeField] private DioramaTimeRowView rowPrefab;

        private readonly List<GameObject> rows = new();

        private void OnEnable() => Refresh();

        private void OnDisable() => ClearRows();

        private void Refresh()
        {
            ClearRows();

            var result = results != null ? results.Current : null;
            if (result == null) return;

            if (blockTitle != null) blockTitle.text = result.Block != null ? result.Block.Title : string.Empty;
            if (totalTime != null) totalTime.text = DioramaTimeFormat.Clock(result.TotalSeconds);
            if (dioramaCount != null) dioramaCount.text = result.DioramaCount.ToString();

            if (recordTime != null)
                recordTime.text = DioramaTimeFormat.Clock(result.IsNewRecord ? result.TotalSeconds : result.BestSeconds);

            if (newRecordBadge != null) newRecordBadge.SetActive(result.IsNewRecord);

            BuildRows(result);
        }

        private void BuildRows(DioramaBlockResult result)
        {
            if (rowContainer == null || rowPrefab == null) return;

            foreach (var entry in result.Dioramas)
            {
                var row = Instantiate(rowPrefab, rowContainer);
                row.Bind(entry.Diorama != null ? entry.Diorama.Title : string.Empty, entry.Seconds);
                rows.Add(row.gameObject);
            }
        }

        private void ClearRows()
        {
            foreach (var row in rows)
                if (row != null) Destroy(row);

            rows.Clear();
        }
    }
}
