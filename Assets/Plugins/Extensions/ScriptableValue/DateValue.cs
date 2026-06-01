using System;
using System.Globalization;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Хранилище даты
    /// </summary>
    [CreateAssetMenu(
        fileName = nameof(DateValue),
        menuName = "Extensions/ScriptableValue/" + nameof(DateValue)
    )]
    public class DateValue : StringValue
    {
        private const string DEFAULT_DATE_FORMAT = "dd.MM.yy";
        
        [SerializeField] private bool useNowAsDefault = false;
        [SerializeField] private string dateFormat = "dd.MM.yy";
        
        /// <summary>
        /// Формат даты
        /// </summary>
        public string DateFormat => dateFormat;

        /// <summary>
        /// Использовать текущую дату как дефолтную
        /// </summary>
        public bool UseNowAsDefault => useNowAsDefault;

        protected override void OnEnable()
        {
            if (useNowAsDefault) defaultValue = DateUtils.Format(DateTime.Now);
            base.OnEnable();
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
#else
        private void OnValidate()
        {
#endif            
            if (string.IsNullOrWhiteSpace(dateFormat))
            {
                dateFormat = DEFAULT_DATE_FORMAT;
            }

            if (useNowAsDefault) return;

            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                defaultValue = DateTime.MinValue.ToString(dateFormat, CultureInfo.InvariantCulture);
            }
        }
        
        /// <summary>
        /// Проверка, установлено ли дефолтное значение даты
        /// </summary>
        public bool IsDefaultDate()
        {
            if (!DateUtils.TryParse(Value, out DateTime valueDate)) return false;
            if (!DateUtils.TryParse(defaultValue, out DateTime defaultDate)) return false;

            return valueDate.Date == defaultDate.Date;
        }
        
        /// <summary>
        /// Установка даты
        /// </summary>
        public override void SetValue(string newValue)
        {
            if (!DateUtils.IsValidDate(newValue))
            {
                ServiceDebug.LogError($"Некорректная дата: {newValue}. Ожидаемый формат: {DateFormat}. Значение сброшено до дефолтного");

                if (!DateUtils.IsValidDate(defaultValue))
                {
                    defaultValue = DateTime.MinValue.ToString(dateFormat, CultureInfo.InvariantCulture);
                }

                base.SetValue(defaultValue);
                return;
            }

            base.SetValue(newValue);
        }

        /// <summary>
        /// Установка даты
        /// </summary>
        public void SetValue(DateTime dateTime) => SetValue(DateUtils.Format(dateTime, DateFormat));
        
        /// <summary>
        /// Очистка даты
        /// </summary>
        public override void Clear()
        {
            base.SetValue(default);
        }
        
        /// <summary>
        /// Попытаться получить дату
        /// </summary>
        public bool TryGetDate(out DateTime result) => DateUtils.TryParse(Value, out result);

        /// <summary>
        /// Получить дату
        /// </summary>
        public DateTime GetDate(DateTime defaultDate = default)
        {
            if (defaultDate == default) defaultDate = DateTime.Now;
            
            if (TryGetDate(out DateTime result)) return result;
            else return defaultDate;
        }
        
        /// <summary>
        /// Проверка дефолтного значения
        /// </summary>
        public bool IsDefaultValueValid() => useNowAsDefault || DateUtils.IsValidDate(defaultValue);

        /// <summary>
        /// Проверка даты
        /// </summary>
        public bool IsDateValid(string value)
        {
            return DateTime.TryParseExact(
                value,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime _
            );
        }
    }
}