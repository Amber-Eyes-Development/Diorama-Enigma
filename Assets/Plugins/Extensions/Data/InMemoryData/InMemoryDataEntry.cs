using System;
using Extensions.Helpers;
using Newtonsoft.Json;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Базовый класс единицы данных
    /// </summary>
    /// <remarks>
    /// Дополняется полями данных
    /// </remarks>
    [Serializable]
    public abstract class InMemoryDataEntry
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        [JsonProperty]
        public string Id { get; private set; } = IdGenerator.NewGuid();

        /// <summary>
        /// Дата создания записи
        /// </summary>
        [JsonProperty]
        public DateTime CreatedAt { get; private set; } = DateTime.Now;

        /// <summary>
        /// Дата последнего обновления записи
        /// </summary>
        [JsonProperty]
        public DateTime UpdatedAt { get; private set; } = DateTime.Now;

        /// <summary>
        /// Обновить дату изменения записи
        /// </summary>
        public void Touch()
        {
            UpdatedAt = DateTime.Now;
            OnTouched(UpdatedAt);
        }
        
        protected virtual void OnTouched(DateTime touchedAt) { }
    }
}