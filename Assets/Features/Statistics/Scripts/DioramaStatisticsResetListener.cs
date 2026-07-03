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

        // Рестарт — новая попытка ПОБИТЬ рекорд: обнуляем накопленное время попытки, но сам рекорд НЕ трогаем
        private void OnBlockRestarted(DioramaBlock block)
        {
            if (block == null) return;

            foreach (var def in access.AllInBlock(block))
                if (def != null) DioramaStatisticsStore.RemoveDioramaSeconds(def.Id);
        }

        private void OnProgressReset() => DioramaStatisticsStore.ClearAll();
    }
}
