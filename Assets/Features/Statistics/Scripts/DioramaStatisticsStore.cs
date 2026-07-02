using System.Collections.Generic;
using Extensions.Data;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Персист статистики прохождения диорам и блоков
    /// Единственное место фичи статистики, знающее о ключах <see cref="JsonSaveLoad"/>
    /// </summary>
    public static class DioramaStatisticsStore
    {
        private const string BLOCK_BEST_TIME_KEY = "statistics.blockBestTime";
        private const string DIORAMA_SECONDS_KEY = "statistics.dioramaSeconds";

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

        /// <summary> Убрать рекорд блока (при рестарте блока) </summary>
        /// <param name="blockId">Идентификатор блока</param>
        public static void RemoveBlockBestTime(string blockId)
        {
            if (string.IsNullOrEmpty(blockId)) return;

            var best = JsonSaveLoad.Load(BLOCK_BEST_TIME_KEY, new Dictionary<string, float>());
            if (best == null || !best.Remove(blockId)) return;

            JsonSaveLoad.Save(best, BLOCK_BEST_TIME_KEY);
        }

        /// <summary> Накопленное время диорамы в секундах </summary>
        /// <param name="dioramaId">Идентификатор диорамы</param>
        public static float LoadDioramaSeconds(string dioramaId)
        {
            if (string.IsNullOrEmpty(dioramaId)) return 0f;

            var seconds = JsonSaveLoad.Load(DIORAMA_SECONDS_KEY, new Dictionary<string, float>());
            return seconds != null && seconds.TryGetValue(dioramaId, out float value) ? value : 0f;
        }

        /// <summary> Добавить время к накопленному времени диорамы </summary>
        /// <param name="dioramaId">Идентификатор диорамы</param>
        /// <param name="delta">Прибавка в секундах</param>
        public static void AddDioramaSeconds(string dioramaId, float delta)
        {
            if (string.IsNullOrEmpty(dioramaId) || delta <= 0f) return;

            var seconds = JsonSaveLoad.Load(DIORAMA_SECONDS_KEY, new Dictionary<string, float>()) ?? new Dictionary<string, float>();
            seconds.TryGetValue(dioramaId, out float value);
            seconds[dioramaId] = value + delta;
            JsonSaveLoad.Save(seconds, DIORAMA_SECONDS_KEY);
        }

        /// <summary> Обнулить накопленное время диорамы </summary>
        /// <param name="dioramaId">Идентификатор диорамы</param>
        public static void RemoveDioramaSeconds(string dioramaId)
        {
            if (string.IsNullOrEmpty(dioramaId)) return;

            var seconds = JsonSaveLoad.Load(DIORAMA_SECONDS_KEY, new Dictionary<string, float>());
            if (seconds == null || !seconds.Remove(dioramaId)) return;

            JsonSaveLoad.Save(seconds, DIORAMA_SECONDS_KEY);
        }

        /// <summary> Стереть все рекорды и накопленное время </summary>
        public static void ClearAll()
        {
            JsonSaveLoad.Save(new Dictionary<string, float>(), BLOCK_BEST_TIME_KEY);
            JsonSaveLoad.Save(new Dictionary<string, float>(), DIORAMA_SECONDS_KEY);
        }
    }
}
