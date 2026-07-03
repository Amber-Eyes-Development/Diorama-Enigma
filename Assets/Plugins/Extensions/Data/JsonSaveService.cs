namespace Extensions.Data
{
    /// <summary>
    /// Реализация <see cref="ISaveService"/> поверх <see cref="JsonSaveLoad"/> (JSON + шифрование, батч-запись)
    /// </summary>
    public sealed class JsonSaveService : ISaveService
    {
        /// <inheritdoc/>
        public bool Save<T>(T data, string key, string profile = null) =>
            JsonSaveLoad.Save(data, key, profile);

        /// <inheritdoc/>
        public T Load<T>(string key, T defaultValue = default, string profile = null) =>
            JsonSaveLoad.Load(key, defaultValue, profile);

        /// <inheritdoc/>
        public bool Exists(string key, string profile = null) =>
            JsonSaveLoad.Exists(key, profile);
    }
}
