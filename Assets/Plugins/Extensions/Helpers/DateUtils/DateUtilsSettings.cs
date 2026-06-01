using System;

namespace Extensions.Helpers
{
    /// <summary>
    /// Настройки форматирования даты
    /// </summary>
    [Serializable]
    public class DateUtilsSettings
    {
        public string DateFormat = "dd.MM.yy";
        public int TwoDigitYearMax = 2099;
    }
}