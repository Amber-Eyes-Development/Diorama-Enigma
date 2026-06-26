using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using Extensions.Log;
using Extensions.UIWindows;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Реакция на завершение блока на игровой сцене: собирает статистику прохождения и открывает окно итогов
    /// </summary>
    public sealed class DioramaBlockCompletionPresenter : MonoBehaviour
    {
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Замерщик времени прохождения")]
        [SerializeField] private DioramaStatisticsRecorder recorder;
        [Tooltip("Носитель итога (ассет-мост в окно)")]
        [SerializeField] private DioramaResultsContext results;
        [Tooltip("Окно итогов блока")]
        [SerializeField] private UIWindowID resultsWindow;

        private void OnEnable()
        {
            if (access == null)
            {
                ServiceDebug.LogError($"{nameof(access)} не назначен");
                return;
            }

            access.onBlockCompleted += OnBlockCompleted;
        }

        private void OnDisable()
        {
            if (access != null) access.onBlockCompleted -= OnBlockCompleted;
        }

        private void OnBlockCompleted(DioramaBlock block)
        {
            if (results != null) results.Set(BuildResult(block));
            OpenResultsWindow();
        }

        // Снять тайминги по диорамам блока, посчитать сумму и сверить/обновить рекорд
        private DioramaBlockResult BuildResult(DioramaBlock block)
        {
            var entries = new List<DioramaBlockResult.Entry>();
            float total = 0f;

            foreach (var def in access.AllInBlock(block))
            {
                float seconds = recorder != null ? recorder.SecondsOf(def) : 0f;
                entries.Add(new DioramaBlockResult.Entry(def, seconds));
                total += seconds;
            }

            float best = DioramaStatisticsStore.LoadBlockBestTime(block.Id);
            bool isNewRecord = best <= 0f || total < best;
            if (isNewRecord) DioramaStatisticsStore.SaveBlockBestTime(block.Id, total);

            return new DioramaBlockResult(block, entries, total, best, isNewRecord);
        }

        private void OpenResultsWindow()
        {
            if (resultsWindow == null)
            {
                ServiceDebug.LogError($"{nameof(resultsWindow)} не назначен");
                return;
            }

            if (UIWindowsController.Instance == null)
            {
                ServiceDebug.LogError($"{nameof(UIWindowsController)} не найден");
                return;
            }

            UIWindowsController.Instance.OpenWindowByID(resultsWindow.Id);
        }
    }
}
