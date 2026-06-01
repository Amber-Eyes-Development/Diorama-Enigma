using System.Globalization;

namespace Extensions.Helpers
{
    /// <summary>
    /// Форматирование данных
    /// </summary>
    public static class Formatters
    {
        /// <summary>
        /// Форматирование float-значения к 12.34
        /// </summary>
        public static string FormatFloat(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        
        /// <summary>
        /// Форматирование string-значения к Hello
        /// </summary>
        public static string FormatString(string value) => char.ToUpper(value[0]) + value[1..];
    }
}