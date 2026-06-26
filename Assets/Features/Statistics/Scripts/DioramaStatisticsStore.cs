using System.Collections.Generic;
using Extensions.Data;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Персист статистики прохождения: рекордное (лучшее) время блоков.
    /// Единственное место фичи статистики, знающее о ключах <see cref="JsonSaveLoad"/>
    /// </summary>
    public static class DioramaStatisticsStore
    {
        private const string BLOCK_BEST_TIME_KEY = "statistics.blockBestTime";

        /// <summary> Рекордное (лучшее) время прохождения блока в секундах (0, если рекорда ещё нет) </summary>
        /// <param name="blockId">Идентификатор блока</param>
        public static float LoadBlockBestTime(string blockId)
        {
            if (string.IsNullOrEmpty(blockId)) return 0f;

            var best = JsonSaveLoad.Load(BLOCK_BEST_TIME_KEY, new Dictionary<string, float>());
            return best != null && best.TryGetValue(blockId, out float seconds) ? seconds : 0f;
        }

        /// <summary> Сохранить рекордное время прохождения блока </summary>
        /// <param name="blockId">Идентификатор блока</param>
        /// <param name="seconds">Время в секундах</param>
        public static void SaveBlockBestTime(string blockId, float seconds)
        {
            if (string.IsNullOrEmpty(blockId)) return;

            var best = JsonSaveLoad.Load(BLOCK_BEST_TIME_KEY, new Dictionary<string, float>()) ?? new Dictionary<string, float>();
            best[blockId] = seconds;
            JsonSaveLoad.Save(best, BLOCK_BEST_TIME_KEY);
        }
    }
}
