using System;
using System.Globalization;
using Extensions.Data;

namespace Extensions.Helpers
{
    /// <summary>
    /// Утилиты для работы с датами
    /// </summary>
    public static class DateUtils
    {
        private const string SETTINGS_SAVE_KEY = "date-utils-settings";

        private static DateUtilsSettings settings;
        private static CultureInfo dateCulture;

        #region Значения параметров настроек дат
        
        /// <summary>
        /// Формат даты
        /// </summary>
        public static string DateFormat => Settings.DateFormat;

        /// <summary>
        /// Максимальный год для двузначного формата
        /// </summary>
        public static int TwoDigitYearMax => Settings.TwoDigitYearMax;
        
        /// <summary>
        /// Текущая дата в формате проекта
        /// </summary>
        public static string Now => Format(DateTime.Now);

        #endregion
        
        #region Настройки даты

        /// <summary>
        /// Загрузить настройки
        /// </summary>
        public static void LoadSettings()
        {
            settings = JsonSaveLoad.Load(SETTINGS_SAVE_KEY, new DateUtilsSettings());
            ValidateSettings();
            RebuildCulture();
        }

        /// <summary>
        /// Сохранить настройки
        /// </summary>
        public static void SaveSettings(string dateFormat, int twoDigitYearMax)
        {
            settings = new DateUtilsSettings
            {
                DateFormat = dateFormat,
                TwoDigitYearMax = twoDigitYearMax
            };

            ValidateSettings();
            RebuildCulture();
            JsonSaveLoad.Save(settings, SETTINGS_SAVE_KEY);
        }

        /// <summary>
        /// Сбросить настройки
        /// </summary>
        public static void ResetSettings()
        {
            DateUtilsSettings defaultSettings = new DateUtilsSettings();
            SaveSettings(defaultSettings.DateFormat, defaultSettings.TwoDigitYearMax);
        }
        
        private static DateUtilsSettings Settings
        {
            get
            {
                if (settings == null) LoadSettings();

                return settings;
            }
        }
        
        private static void ValidateSettings()
        {
            if (settings == null) 
                settings = new DateUtilsSettings();

            if (string.IsNullOrWhiteSpace(settings.DateFormat))
                settings.DateFormat = new DateUtilsSettings().DateFormat;

            if (settings.TwoDigitYearMax < 2000)
                settings.TwoDigitYearMax = new DateUtilsSettings().TwoDigitYearMax;
        }
        
        #endregion

        #region Парсинг

        /// <summary>
        /// Получить дату
        /// </summary>
        public static bool TryParse(string value, out DateTime result)
        {
            return DateTime.TryParseExact(
                value,
                DateFormat,
                dateCulture,
                DateTimeStyles.None,
                out result
            );
        }

        #endregion
        
        #region Форматирование
        
        /// <summary>
        /// Форматировать дату
        /// </summary>
        public static string Format(DateTime dateTime) => dateTime.ToString(DateFormat, dateCulture);
        
        /// <summary>
        /// Форматировать дату
        /// </summary>
        public static string Format(DateTime dateTime, string format) => dateTime.ToString(format, dateCulture);
        
        #endregion
        
        #region Проверки

        /// <summary>
        /// Проверка даты
        /// </summary>
        public static bool IsValidDate(string value) => TryParse(value, out DateTime _);
        
        /// <summary>
        /// Проверить, пересекаются ли два интервала дат
        /// </summary>
        public static bool IsDateRangesIntersect(
            DateTime firstStart, DateTime firstEnd, 
            DateTime secondStart, DateTime secondEnd)
        {
            return firstStart.Date <= secondEnd.Date && secondStart.Date <= firstEnd.Date;
        }
        
        #endregion

        #region Внутренние методы
        
        private static void RebuildCulture()
        {
            CultureInfo cultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();

            GregorianCalendar calendar = new GregorianCalendar();
            calendar.TwoDigitYearMax = Settings.TwoDigitYearMax;

            cultureInfo.DateTimeFormat.Calendar = calendar;
            dateCulture = cultureInfo;
        }
        
        #endregion
    }
}