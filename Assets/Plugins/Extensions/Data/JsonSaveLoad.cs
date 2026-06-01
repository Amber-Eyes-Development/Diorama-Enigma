using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks; // UniTask
using Extensions.Log;
using Newtonsoft.Json; // Newtonsoft.Json
using UnityEngine;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace Extensions.Data
{
    /// <summary>
    /// Сохранение/загрузка в JSON с шифрованием
    /// <remarks>
    /// Для хранения игровых данных и структур настроек
    /// Поддерживает как синхронный API (через кэш), так и асинхронный
    /// </remarks>
    /// </summary>
    public static class JsonSaveLoad
    {
        #region Constants
        
        private const string SAVE_FOLDER = "SaveData";
        private const string FILE_EXTENSION = ".sav";
        private const string BACKUP_EXTENSION = ".bak";
        private const string TEMP_EXTENSION = ".tmp";
        private const int CURRENT_VERSION = 1;
        private const string DEFAULT_PROFILE_NAME = "default";
        private const string DEVELOPER_PROFILE_NAME = "developer";
        private const string SAVE_FILE_NAME = "save";
        
        #endregion

        #region Events
        
        /// <summary>
        /// Событие перед началом процесса сохранения
        /// </summary>
        public static event Action<string> onBeforeSave;
        /// <summary>
        /// Событие после успешного завершения процесса сохранения
        /// </summary>
        public static event Action<string> onAfterSave;
        /// <summary>
        /// Событие ошибки процесса сохранения
        /// </summary>
        public static event Action<string, Exception> onSaveError;
        
        /// <summary>
        /// Событие перед началом процесса загрузки
        /// </summary>
        public static event Action<string> onBeforeLoad;
        /// <summary>
        /// Событие после успешного завершения процесса загрузки
        /// </summary>
        public static event Action<string> onAfterLoad;
        /// <summary>
        /// Событие ошибки процесса загрузки
        /// </summary>
        public static event Action<string, Exception> onLoadError;
        
        #endregion
        
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new DefaultContractResolver()
        };
        
        private static int savingCount;
        private static readonly Dictionary<string, SemaphoreSlim> fileLocks = new Dictionary<string, SemaphoreSlim>();
        
        // Кэш загруженных данных для синхронного API
        private static readonly Dictionary<string, object> dataCache = new Dictionary<string, object>();
        private static readonly Dictionary<string, UniTask> loadingTasks = new Dictionary<string, UniTask>();

        /// <summary>
        /// Состояние процесса сохранения
        /// </summary>
        public static bool IsSaving => savingCount > 0;
        
        /// <summary>
        /// Активный профиль сохранения
        /// </summary>
        public static string CurrentProfile { get; set; } = DEFAULT_PROFILE_NAME;
        
        /// <summary>
        /// Профиль сохранения данных разработчика
        /// </summary>
        public static string DeveloperProfile { get; set; } = DEVELOPER_PROFILE_NAME;
        
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

        private static string CurrentProfileDirectory => GetProfileDirectory(CurrentProfile);
        
        [Serializable]
        private class MultiSaveContainer
        {
            public int Version;
            public string Profile;
            public string Hash;
            public string TimestampUtc;
            public MultiSaveEntry[] Entries;
        }

        [Serializable]
        private class MultiSaveEntry
        {
            public string Key;
            public string DataType;
            public string DataJson;
        }

        static JsonSaveLoad()
        {
            serializerSettings.Converters.Add(new Vector2Converter());
            serializerSettings.Converters.Add(new Vector3Converter());
            serializerSettings.Converters.Add(new QuaternionConverter());
            serializerSettings.Converters.Add(new ColorConverter());
            
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
                ServiceDebug.LogWarning($"Путь «{SaveDirectory}» не существует, создана новая директория");
            }
        }

        #region Sync API

        /// <summary>
        /// Сохранить данные в запись по ключу сохранения (синхронно, через кэш)
        /// </summary>
        /// <param name="data">Сохраняемые данные</param>
        /// <param name="key">Название ключа сохранения</param>
        /// <returns></returns>
        public static bool Save<T>(T data, string key, string profile = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                ServiceDebug.LogError("Пустое имя файла, сохранение не выполнено");
                return false;
            }

            string cacheKey = GetCacheKey(key, profile);
            dataCache[cacheKey] = data;

            SaveAsync(data, key, profile).Forget();
            
            return true;
        }

        /// <summary>
        /// Загрузить данные из записи по ключу сохранения (синхронно, из кэша или с диска)
        /// </summary>
        /// <param name="key">Название ключа сохранения</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        /// <typeparam name="T">Загружаемые данные</typeparam>
        /// <returns></returns>
        public static T Load<T>(string key, T defaultValue = default, string profile = null)
        {
            string cacheKey = GetCacheKey(key, profile);
            if (dataCache.TryGetValue(cacheKey, out object cachedData)) return (T)cachedData;

            if (loadingTasks.ContainsKey(cacheKey))
            {
                ServiceDebug.LogWarning(
                    $"Синхронный Load вызван во время async загрузки файла «{key}». Используйте LoadAsync или EnsureLoadedAsync.");
                return defaultValue;
            }

            try
            {
                LoadInternalSync(key, defaultValue, cacheKey, profile);

                if (dataCache.TryGetValue(cacheKey, out object loadedData)) return (T)loadedData;
                return defaultValue;
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"[JsonSaveLoad] Ошибка синхронной загрузки файла «{key}»: {ex}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Проверить существование записи по ключу сохранения (синхронно)
        /// </summary>
        public static bool Exists(string key, string profile = null)
        {
            if (string.IsNullOrEmpty(key)) return false;

            string cacheKey = GetCacheKey(key, profile);
            if (dataCache.ContainsKey(cacheKey)) return true;

            try
            {
                MultiSaveContainer container = TryLoadMultiContainerSync(profile);
                if (container == null) return false;

                if (!ValidateMultiHash(container))
                {
                    container = TryLoadMultiBackupContainerSync(profile);
                    if (container == null) return false;
                    if (!ValidateMultiHash(container)) return false;
                }

                MultiSaveEntry entry = GetEntry(container, key);
                return entry != null;
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"Ошибка синхронной проверки Exists для ключа «{key}»: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Предзагрузить данные асинхронно (для избежания блокировки в Load)
        /// </summary>
        public static async UniTask PreloadAsync<T>(string key, T defaultValue = default, string profile = null)
        {
            string cacheKey = GetCacheKey(key, profile);

            if (dataCache.ContainsKey(cacheKey)) return;

            var _ = await LoadAsync(key, defaultValue, profile);
            // Данные уже будут в кэше после LoadAsync
        }

        /// <summary>
        /// Очистить кэш
        /// </summary>
        public static void ClearCache()
        {
            dataCache.Clear();
            ServiceDebug.Log("Кэш JsonSaveLoad очищен");
        }

        /// <summary>
        /// Удалить из кэша конкретную запись по ключу
        /// </summary>
        public static void InvalidateCache(string key, string profile = null)
        {
            string cacheKey = GetCacheKey(key, profile);
            dataCache.Remove(cacheKey);
        }

        private static string ResolveProfile(string profile = null)
        {
            if (!string.IsNullOrEmpty(profile)) return profile;
            if (!string.IsNullOrEmpty(CurrentProfile)) return CurrentProfile;
            return DEFAULT_PROFILE_NAME;
        }

        private static string GetProfileDirectory(string profile = null)
        {
            string resolvedProfile = ResolveProfile(profile);
            string path = Path.Combine(SaveDirectory, resolvedProfile);

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return path;
        }

        private static string GetCacheKey(string key, string profile = null)
        {
            string resolvedProfile = ResolveProfile(profile);
            return $"{resolvedProfile}:{key}";
        }

        #endregion

        #region Async API

        /// <summary>
        /// Сохранить данные в файл сохранения (асинхронно)
        /// </summary>
        /// <param name="data">Сохраняемые данные</param>
        /// <param name="key">Название файла сохранения</param>
        /// <returns></returns>
        public static async UniTask<bool> SaveAsync<T>(T data, string key, string profile = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                ServiceDebug.LogError("Пустое имя файла, сохранение не выполнено");
                return false;
            }

            savingCount++;
            SemaphoreSlim fileLock = GetFileLock(profile);
            await fileLock.WaitAsync();
            
            try
            {
                string resolvedProfile = ResolveProfile(profile);
                onBeforeSave?.Invoke(key);

                string cacheKey = GetCacheKey(key, resolvedProfile);
                dataCache[cacheKey] = data;

                MultiSaveContainer container = await TryLoadMultiContainerAsync(resolvedProfile) ?? new MultiSaveContainer
                {
                    Version = CURRENT_VERSION,
                    Profile = resolvedProfile,
                    TimestampUtc = DateTime.UtcNow.ToString("o"),
                    Entries = Array.Empty<MultiSaveEntry>()
                };

                MultiSaveEntry entry = new MultiSaveEntry
                {
                    Key = key,
                    DataType = typeof(T).AssemblyQualifiedName,
                    DataJson = JsonConvert.SerializeObject(data, Formatting.None, serializerSettings)
                };

                UpsertEntry(container, entry);
                container.Version = CURRENT_VERSION;
                container.Profile = resolvedProfile;
                container.TimestampUtc = DateTime.UtcNow.ToString("o");

                string hashPayload = ComputeMultiPayloadHash(container);
                container.Hash = hashPayload;

                string json = JsonConvert.SerializeObject(container, Formatting.Indented, serializerSettings);
                string encrypted = DataEncryptor.Encrypt(json, DataEncryptor.EncryptionMode);

                string filePath = Path.Combine(GetProfileDirectory(resolvedProfile), SAVE_FILE_NAME + FILE_EXTENSION);
                string backupPath = filePath + BACKUP_EXTENSION;
                string tempPath = filePath + TEMP_EXTENSION;

                try
                {
                    await File.WriteAllTextAsync(tempPath, encrypted);

                    if (File.Exists(filePath))
                    {
                        File.Copy(filePath, backupPath, true);
                        File.Delete(filePath);
                    }

                    File.Move(tempPath, filePath);

                    onAfterSave?.Invoke(key);
                }
                catch (Exception ex)
                {
                    ServiceDebug.LogError($"Ошибка сохранения файла «{SAVE_FILE_NAME}»: {ex}");

                    if (File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); }
                        catch
                        {
                            // ignored
                        }
                    }

                    onSaveError?.Invoke(key, ex);
                    return false;
                }

                return true;
            }
            finally
            {
                fileLock.Release();
                savingCount--;
                if (savingCount < 0)
                {
                    savingCount = 0;
                }
            }
        }

        /// <summary>
        /// Загрузить данные из записи по ключу сохранения (асинхронно)
        /// </summary>
        /// <param name="key">Название ключа сохранения</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        /// <typeparam name="T">Загружаемые данные</typeparam>
        /// <returns></returns>
        public static async UniTask<T> LoadAsync<T>(string key, T defaultValue = default, string profile = null)
        {
            string cacheKey = GetCacheKey(key, profile);

            if (dataCache.TryGetValue(cacheKey, out object cachedData))
            {
                return (T)cachedData;
            }

            if (loadingTasks.TryGetValue(cacheKey, out UniTask existingTask))
            {
                await existingTask;
                if (dataCache.TryGetValue(cacheKey, out object loadedData))
                {
                    return (T)loadedData;
                }
            }

            UniTask loadTask = LoadInternalAsync(key, defaultValue, cacheKey, profile);
            loadingTasks[cacheKey] = loadTask;

            try
            {
                await loadTask;
            }
            finally
            {
                loadingTasks.Remove(cacheKey);
            }

            if (dataCache.TryGetValue(cacheKey, out object finalData))
            {
                return (T)finalData;
            }

            return defaultValue;
        }
        
        private static async UniTask LoadInternalAsync<T>(string key, T defaultValue, string cacheKey, string profile = null)
        {
            onBeforeLoad?.Invoke(key);

            MultiSaveContainer container = await ResolveValidContainerAsync(profile);
            if (container == null)
            {
                dataCache[cacheKey] = defaultValue;
                onAfterLoad?.Invoke(key);
                return;
            }

            LoadFromContainer(key, defaultValue, cacheKey, container);
        }

        /// <summary>
        /// Проверить существование записи по ключу сохранения (асинхронно)
        /// </summary>
        /// <param name="key">Название ключа сохранения</param>
        /// <returns></returns>
        public static async UniTask<bool> ExistsAsync(string key, string profile = null)
        {
            MultiSaveContainer container = await TryLoadMultiContainerAsync(profile);
            if (container == null)
            {
                return false;
            }

            MultiSaveEntry entry = GetEntry(container, key);
            return entry != null;
        }

        /// <summary>
        /// Удалить запись по ключу сохранения (асинхронно)
        /// </summary>
        /// <param name="key">Название ключ сохранения</param>
        /// <returns></returns>
        public static async UniTask<bool> DeleteAsync(string key, string profile = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                ServiceDebug.LogError("Пустое имя файла, удаление не выполнено");
                return false;
            }
            
            SemaphoreSlim fileLock = GetFileLock(profile);
            await fileLock.WaitAsync();

            try
            {
                string resolvedProfile = ResolveProfile(profile);
                string cacheKey = GetCacheKey(key, resolvedProfile);
                dataCache.Remove(cacheKey);

                MultiSaveContainer container = await TryLoadMultiContainerAsync(resolvedProfile);
                if (container == null)
                {
                    ServiceDebug.LogWarning($"Файл «{SAVE_FILE_NAME}» не найден, удаление не выполнено");
                    return false;
                }

                if (!RemoveEntry(container, key))
                {
                    ServiceDebug.LogWarning($"Файл «{key}» не найден в контейнере, удаление не выполнено");
                    return false;
                }

                container.Version = CURRENT_VERSION;
                container.Profile = resolvedProfile;
                container.TimestampUtc = DateTime.UtcNow.ToString("o");

                string hashPayload = ComputeMultiPayloadHash(container);
                container.Hash = hashPayload;

                string json = JsonConvert.SerializeObject(container, Formatting.Indented, serializerSettings);
                string encrypted = DataEncryptor.Encrypt(json, DataEncryptor.EncryptionMode);

                string filePath = Path.Combine(GetProfileDirectory(resolvedProfile), SAVE_FILE_NAME + FILE_EXTENSION);
                string backupPath = filePath + BACKUP_EXTENSION;
                string tempPath = filePath + TEMP_EXTENSION;

                try
                {
                    await File.WriteAllTextAsync(tempPath, encrypted);

                    if (File.Exists(filePath))
                    {
                        File.Copy(filePath, backupPath, true);
                        File.Delete(filePath);
                    }

                    File.Move(tempPath, filePath);

                    return true;
                }
                catch (Exception ex)
                {
                    ServiceDebug.LogError($"Ошибка удаления файла «{key}»: {ex}");

                    if (File.Exists(tempPath))
                    {
                        try
                        {
                            File.Delete(tempPath);
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    return false;
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <summary>
        /// Удалить все файлы сохранений текущего профиля (асинхронно)
        /// </summary>
        public static async UniTask DeleteAllAsync(string profile = null)
        {
            SemaphoreSlim fileLock = GetFileLock(profile);
            await fileLock.WaitAsync();

            try
            {
                string resolvedProfile = ResolveProfile(profile);
                string profilePrefix = $"{resolvedProfile}:";
                List<string> keysToRemove = new List<string>();

                foreach (string key in dataCache.Keys)
                {
                    if (key.StartsWith(profilePrefix))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (string key in keysToRemove)
                {
                    dataCache.Remove(key);
                }

                string profileDir = GetProfileDirectory(resolvedProfile);
                if (Directory.Exists(profileDir))
                {
                    await UniTask.RunOnThreadPool(() => Directory.Delete(profileDir, true));
                    ServiceDebug.Log($"Все сохранения профиля «{resolvedProfile}» удалены");
                }
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"Ошибка удаления всех сохранений: {ex}");
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <summary>
        /// Получить список всех ключей в контейнере (асинхронно)
        /// </summary>
        public static async UniTask<string[]> GetAllKeysAsync(string profile = null)
        {
            MultiSaveContainer container = await TryLoadMultiContainerAsync(profile);
            if (container == null || container.Entries == null)
            {
                return Array.Empty<string>();
            }

            string[] keys = new string[container.Entries.Length];
            for (int i = 0; i < container.Entries.Length; i++)
            {
                keys[i] = container.Entries[i]?.Key ?? string.Empty;
            }

            return keys;
        }

        #endregion

        #region Internal Methods (Save/Load/Parse)

        private static MultiSaveContainer ParseContainerFromEncrypted(string encrypted, string context)
        {
            try
            {
                string json = DataEncryptor.Decrypt(encrypted, DataEncryptor.EncryptionMode);

                if (string.IsNullOrEmpty(json))
                {
                    ServiceDebug.LogError($"{context} (пустой JSON)");
                    return null;
                }

                return JsonConvert.DeserializeObject<MultiSaveContainer>(json, serializerSettings);
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"{context}: {ex}");
                return null;
            }
        }
        
        private static void LoadFromContainer<T>(
            string key,
            T defaultValue,
            string cacheKey,
            MultiSaveContainer container)
        {
            if (container == null)
            {
                ServiceDebug.LogWarning($"Файл «{SAVE_FILE_NAME}» не найден, загружены значения по-умолчанию");
                dataCache[cacheKey] = defaultValue;
                onAfterLoad?.Invoke(key);
                return;
            }

            ValidateVersion(container, SAVE_FILE_NAME);

            MultiSaveEntry entry = GetEntry(container, key);
            if (entry == null)
            {
                ServiceDebug.LogWarning($"Ключ «{key}» не найден в файле, загружены значения по-умолчанию");
                dataCache[cacheKey] = defaultValue;
                onAfterLoad?.Invoke(key);
                return;
            }

            try
            {
                T result = JsonConvert.DeserializeObject<T>(entry.DataJson, serializerSettings);
                dataCache[cacheKey] = result;
                onAfterLoad?.Invoke(key);
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"Ошибка десериализации файла (ключ «{key}»): {ex}");
                dataCache[cacheKey] = defaultValue;
                onLoadError?.Invoke(key, ex);
            }
        }
        
        private static MultiSaveContainer ResolveValidContainerSync(string profile = null)
        {
            MultiSaveContainer container = TryLoadMultiContainerSync(profile);
            if (container == null)
            {
                return null;
            }

            if (!ValidateMultiHash(container))
            {
                ServiceDebug.LogError($"Хэш-подпись файла «{SAVE_FILE_NAME}» не совпадает, попытка восстановления из бэкапа");

                container = TryLoadMultiBackupContainerSync(profile);
                if (container == null)
                {
                    return null;
                }

                if (!ValidateMultiHash(container))
                {
                    ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось (хэш не совпадает)");
                    return null;
                }
            }

            return container;
        }

        private static async UniTask<MultiSaveContainer> ResolveValidContainerAsync(string profile = null)
        {
            MultiSaveContainer container = await TryLoadMultiContainerAsync(profile);
            if (container == null)
            {
                return null;
            }

            if (!ValidateMultiHash(container))
            {
                ServiceDebug.LogError($"Хэш-подпись файла «{SAVE_FILE_NAME}» не совпадает, попытка восстановления из бэкапа");

                container = await TryLoadMultiBackupContainerAsync(profile);
                if (container == null)
                {
                    return null;
                }

                if (!ValidateMultiHash(container))
                {
                    ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось (хэш не совпадает)");
                    return null;
                }
            }

            return container;
        }
        
        private static async UniTask<MultiSaveContainer> TryLoadMultiContainerAsync(string profile = null)
        {
            string filePath = Path.Combine(GetProfileDirectory(profile), SAVE_FILE_NAME + FILE_EXTENSION);
            if (!File.Exists(filePath)) return null;

            string encrypted = await File.ReadAllTextAsync(filePath);
            return ParseContainerFromEncrypted(encrypted, $"Ошибка загрузки файла «{SAVE_FILE_NAME}»");
        }

        private static async UniTask<MultiSaveContainer> TryLoadMultiBackupContainerAsync(string profile = null)
        {
            string backupPath = Path.Combine(GetProfileDirectory(profile), SAVE_FILE_NAME + FILE_EXTENSION + BACKUP_EXTENSION);

            if (!File.Exists(backupPath))
            {
                ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, бэкап не найден");
                return null;
            }

            try
            {
                string encrypted = await File.ReadAllTextAsync(backupPath);
                string json = DataEncryptor.Decrypt(encrypted, DataEncryptor.EncryptionMode);

                if (string.IsNullOrEmpty(json))
                {
                    ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось (пустой JSON)");
                    return null;
                }

                MultiSaveContainer container = JsonConvert.DeserializeObject<MultiSaveContainer>(json);
                if (container == null)
                {
                    ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось (ошибка парсинга)");
                    return null;
                }

                return container;
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось: {ex}");
                return null;
            }
        }
        
        
        private static void LoadInternalSync<T>(string key, T defaultValue, string cacheKey, string profile = null)
        {
            onBeforeLoad?.Invoke(key);

            MultiSaveContainer container = ResolveValidContainerSync(profile);
            if (container == null)
            {
                dataCache[cacheKey] = defaultValue;
                onAfterLoad?.Invoke(key);
                return;
            }

            LoadFromContainer(key, defaultValue, cacheKey, container);
        }

        private static MultiSaveContainer TryLoadMultiContainerSync(string profile = null)
        {
            string filePath = Path.Combine(GetProfileDirectory(profile), SAVE_FILE_NAME + FILE_EXTENSION);
            if (!File.Exists(filePath)) return null;

            string encrypted = File.ReadAllText(filePath);
            return ParseContainerFromEncrypted(encrypted, $"Ошибка загрузки файла «{SAVE_FILE_NAME}»");
        }


        private static MultiSaveContainer TryLoadMultiBackupContainerSync(string profile = null)
        {
            string backupPath = Path.Combine(GetProfileDirectory(profile), SAVE_FILE_NAME + FILE_EXTENSION + BACKUP_EXTENSION);

            if (!File.Exists(backupPath))
            {
                ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, бэкап не найден");
                return null;
            }

            try
            {
                string encrypted = File.ReadAllText(backupPath);
                string json = DataEncryptor.Decrypt(encrypted, DataEncryptor.EncryptionMode);

                if (string.IsNullOrEmpty(json))
                {
                    ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось (пустой JSON)");
                    return null;
                }

                MultiSaveContainer container = JsonConvert.DeserializeObject<MultiSaveContainer>(json);
                if (container == null)
                {
                    ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось (ошибка парсинга)");
                    return null;
                }

                return container;
            }
            catch (Exception ex)
            {
                ServiceDebug.LogError($"Файл «{SAVE_FILE_NAME}» поврежден, восстановление из бэкапа не удалось: {ex}");
                return null;
            }
        }
        
        #endregion

        #region Internal Methods (Data)

        private static void UpsertEntry(MultiSaveContainer container, MultiSaveEntry newEntry)
        {
            if (container == null || newEntry == null || string.IsNullOrEmpty(newEntry.Key))
            {
                return;
            }

            if (container.Entries == null)
            {
                container.Entries = new[] { newEntry };
                return;
            }

            int length = container.Entries.Length;
            int replaceIndex = -1;

            for (int i = 0; i < length; i++)
            {
                MultiSaveEntry entry = container.Entries[i];
                if (entry != null && string.Equals(entry.Key, newEntry.Key, StringComparison.Ordinal))
                {
                    replaceIndex = i;
                    break;
                }
            }

            if (replaceIndex >= 0)
            {
                container.Entries[replaceIndex] = newEntry;
            }
            else
            {
                MultiSaveEntry[] newEntries = new MultiSaveEntry[length + 1];
                Array.Copy(container.Entries, newEntries, length);
                newEntries[length] = newEntry;
                container.Entries = newEntries;
            }
        }

        private static bool RemoveEntry(MultiSaveContainer container, string key)
        {
            if (container == null || container.Entries == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            int length = container.Entries.Length;
            int removeIndex = -1;

            for (int i = 0; i < length; i++)
            {
                MultiSaveEntry entry = container.Entries[i];
                if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    removeIndex = i;
                    break;
                }
            }

            if (removeIndex < 0)
            {
                return false;
            }

            if (length == 1)
            {
                container.Entries = Array.Empty<MultiSaveEntry>();
                return true;
            }

            MultiSaveEntry[] newEntries = new MultiSaveEntry[length - 1];
            int writeIndex = 0;

            for (int i = 0; i < length; i++)
            {
                if (i == removeIndex)
                {
                    continue;
                }

                newEntries[writeIndex] = container.Entries[i];
                writeIndex++;
            }

            container.Entries = newEntries;
            return true;
        }

        private static MultiSaveEntry GetEntry(MultiSaveContainer container, string key)
        {
            if (container == null || container.Entries == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            int length = container.Entries.Length;
            for (int i = 0; i < length; i++)
            {
                MultiSaveEntry entry = container.Entries[i];
                if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool ValidateMultiHash(MultiSaveContainer container)
        {
            if (container == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(container.Hash))
            {
                return true;
            }

            string expected = ComputeMultiPayloadHash(container);
            bool isValid = string.Equals(expected, container.Hash, StringComparison.Ordinal);
            return isValid;
        }

        private static string ComputeMultiPayloadHash(MultiSaveContainer container)
        {
            if (container == null || container.Entries == null)
            {
                return string.Empty;
            }

            MultiSaveEntry[] entries = new MultiSaveEntry[container.Entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = container.Entries[i];
            }

            Array.Sort(entries, (a, b) =>
            {
                string ak = a == null ? string.Empty : a.Key;
                string bk = b == null ? string.Empty : b.Key;
                return string.Compare(ak, bk, StringComparison.Ordinal);
            });

            StringBuilder sb = new StringBuilder(entries.Length * 64);
            for (int i = 0; i < entries.Length; i++)
            {
                MultiSaveEntry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                sb.Append(entry.Key);
                sb.Append('|');
                sb.Append(entry.DataType);
                sb.Append('|');
                sb.Append(entry.DataJson);
                sb.Append('\n');
            }

            string payload = sb.ToString();
            string hash = ComputeHash(payload);
            return hash;
        }

        private static string ComputeHash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(input);

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(dataBytes);
                int length = hashBytes.Length;
                char[] chars = new char[length * 2];

                const string hex = "0123456789ABCDEF";

                for (int i = 0; i < length; i++)
                {
                    int b = hashBytes[i];
                    chars[i * 2] = hex[b >> 4];
                    chars[i * 2 + 1] = hex[b & 0xF];
                }

                string result = new string(chars);
                return result;
            }
        }

        private static void ValidateVersion(MultiSaveContainer container, string fileName)
        {
            if (container == null)
            {
                return;
            }

            if (container.Version != CURRENT_VERSION)
            {
                ServiceDebug.LogWarning($"Версия сохранения «{fileName}» ({container.Version}) не совпадает с текущей ({CURRENT_VERSION})");
            }
        }
        
        private static SemaphoreSlim GetFileLock(string profile = null)
        {
            string resolvedProfile = ResolveProfile(profile);
            string lockKey = resolvedProfile;

            lock (fileLocks)
            {
                if (!fileLocks.TryGetValue(lockKey, out SemaphoreSlim sem))
                {
                    sem = new SemaphoreSlim(1, 1);
                    fileLocks[lockKey] = sem;
                }

                return sem;
            }
        }
        
        #endregion
    }
}
