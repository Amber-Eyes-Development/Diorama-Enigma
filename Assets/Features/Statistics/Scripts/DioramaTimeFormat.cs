using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Форматирование длительностей статистики в человекочитаемый вид
    /// </summary>
    public static class DioramaTimeFormat
    {
        /// <summary> Время в формате m:ss (или h:mm:ss для часовых прогонов) </summary>
        /// <param name="seconds">Длительность в секундах</param>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;

            int total = Mathf.FloorToInt(seconds);
            int hours = total / 3600;
            int minutes = total % 3600 / 60;
            int secs = total % 60;

            return hours > 0
                ? $"{hours}:{minutes:00}:{secs:00}"
                : $"{minutes}:{secs:00}";
        }
    }
}
