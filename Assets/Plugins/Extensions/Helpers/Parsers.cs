using System.Globalization;

namespace Extensions.Helpers
{
    /// <summary>
    /// Парсеры данных
    /// </summary>
    public static class Parsers
    {
        /// <summary>
        /// Парсит дробное значение из строки
        /// </summary>
        /// <param name="text">Строка</param>
        /// <param name="defaultValue">Дефолтное значение на случай ошибки</param>
        /// <returns>Дробное значение или дефолтное значение</returns>
        public static float ParseFloat(string text, float defaultValue = 0f)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return defaultValue;
            }

            string normalizedText = text.Trim().Replace(',', '.');

            if (!float.TryParse(
                    normalizedText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result))
            {
                return defaultValue;
            }

            if (float.IsNaN(result) || float.IsInfinity(result))
            {
                return defaultValue;
            }

            return result;
        }
        
        /// <summary>
        /// Парсит целочисленное значение из строки
        /// </summary>
        /// <param name="text">Строка</param>
        /// <param name="defaultValue">Дефолтное значение на случай ошибки</param>
        /// <returns>Целочисленное значение или дефолтное значение</returns>
        public static int ParseInt(string text, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return defaultValue;
            }

            string normalizedText = text.Trim();

            if (!int.TryParse(
                    normalizedText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result))
            {
                return defaultValue;
            }

            return result;
        }
    }
}