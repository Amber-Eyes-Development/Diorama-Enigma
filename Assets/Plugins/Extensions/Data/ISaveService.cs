namespace Extensions.Data
{
    /// <summary>
    /// Общий шов сохранения/загрузки для любой системы
    /// </summary>
    /// <remarks>
    /// Синхронный API по образцу <see cref="JsonSaveLoad"/>: данные доступны сразу, запись на диск — на усмотрение реализации
    /// </remarks>
    public interface ISaveService
    {
        /// <summary>
        /// Сохранить данные по ключу
        /// </summary>
        /// <param name="data">Сохраняемые данные</param>
        /// <param name="key">Ключ записи</param>
        /// <param name="profile">Профиль (слот) сохранения; null — активный</param>
        /// <returns>true, если запрос принят</returns>
        bool Save<T>(T data, string key, string profile = null);

        /// <summary>
        /// Загрузить данные по ключу
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по умолчанию, если записи нет</param>
        /// <param name="profile">Профиль (слот) сохранения; null — активный</param>
        T Load<T>(string key, T defaultValue = default, string profile = null);

        /// <summary>
        /// Существует ли запись по ключу
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="profile">Профиль (слот) сохранения; null — активный</param>
        bool Exists(string key, string profile = null);
    }
}
