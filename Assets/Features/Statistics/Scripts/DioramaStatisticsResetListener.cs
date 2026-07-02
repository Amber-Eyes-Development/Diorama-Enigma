using DioramaEnigma.Dioramas;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Держит рекорды в согласии со сбросами прогресса: рестарт блока стирает его рекорд, сброс всего — все рекорды
    /// </summary>
    public sealed class DioramaStatisticsResetListener : MonoBehaviour
    {
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;

        private void OnEnable()
        {
            if (access == null)
            {
                ServiceDebug.LogError($"{nameof(access)} не назначен");
                return;
            }

            access.onBlockRestarted += OnBlockRestarted;
            access.onProgressReset += OnProgressReset;
        }

        private void OnDisable()
        {
            if (access == null) return;

            access.onBlockRestarted -= OnBlockRestarted;
            access.onProgressReset -= OnProgressReset;
        }

        private void OnBlockRestarted(DioramaBlock block)
        {
            if (block == null) return;

            DioramaStatisticsStore.RemoveBlockBestTime(block.Id);
            foreach (var def in access.AllInBlock(block))
                if (def != null) DioramaStatisticsStore.RemoveDioramaSeconds(def.Id);
        }

        private void OnProgressReset() => DioramaStatisticsStore.ClearAll();
    }
}
