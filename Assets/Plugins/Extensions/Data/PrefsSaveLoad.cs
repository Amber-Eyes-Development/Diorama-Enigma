using Extensions.Log;
using UnityEngine;

namespace Extensions.Data
{
    /// <summary>
    /// Сохранение/загрузка в PlayerPrefs
    /// <remarks>
    /// Для служебных решений и editor-скриптов
    /// </remarks>
    /// </summary>
    public static class PrefsSaveLoad
    {
        private const int TRUE_INT = 1;
        private const int FALSE_INT = 0;

        /// <summary>
        /// Полная очитска всех префсов
        /// </summary>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Удаление определенной записи префсов
        /// </summary>
        /// <param name="key">Ключ записи</param>
        public static void DeleteKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                ServiceDebug.LogWarning($"Ключ «{key}» не найден в PlayerPrefs");
                return;
            }

            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Существует ли ключ
        /// </summary>
        /// <param name="key">Ключ записи</param>
        public static bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return false;
            }

            return PlayerPrefs.HasKey(key);
        }
        
        /// <summary>
        /// Получить int значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static int GetInt(string key, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            return PlayerPrefs.GetInt(key, defaultValue);
        }

        /// <summary>
        /// Назначить int значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetInt(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Получить float значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static float GetFloat(string key, float defaultValue = 0.0f)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        /// <summary>
        /// Назначить float значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetFloat(string key, float value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Получить string значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static string GetString(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            return PlayerPrefs.GetString(key, defaultValue);
        }

        /// <summary>
        /// Назначить string значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            if (value == null)
            {
                value = string.Empty;
            }

            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Получить bool значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            int defaultInt = defaultValue ? TRUE_INT : FALSE_INT;
            int value = GetInt(key, defaultInt);
            return value != 0;
        }

        /// <summary>
        /// Назначить bool значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetBool(string key, bool value)
        {
            int intValue = value ? TRUE_INT : FALSE_INT;
            SetInt(key, intValue);
        }

        /// <summary>
        /// Получить Vector2 значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static Vector2 GetVector2(string key, Vector2 defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            float x = PlayerPrefs.GetFloat(key + ".x", defaultValue.x);
            float y = PlayerPrefs.GetFloat(key + ".y", defaultValue.y);

            Vector2 result = new Vector2(x, y);
            return result;
        }

        /// <summary>
        /// Назначить Vector2 значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetVector2(string key, Vector2 value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            PlayerPrefs.SetFloat(key + ".x", value.x);
            PlayerPrefs.SetFloat(key + ".y", value.y);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Получить Vector3 значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static Vector3 GetVector3(string key, Vector3 defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            float x = PlayerPrefs.GetFloat(key + ".x", defaultValue.x);
            float y = PlayerPrefs.GetFloat(key + ".y", defaultValue.y);
            float z = PlayerPrefs.GetFloat(key + ".z", defaultValue.z);

            Vector3 result = new Vector3(x, y, z);
            return result;
        }

        /// <summary>
        /// Назначить Vector3 значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetVector3(string key, Vector3 value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            PlayerPrefs.SetFloat(key + ".x", value.x);
            PlayerPrefs.SetFloat(key + ".y", value.y);
            PlayerPrefs.SetFloat(key + ".z", value.z);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Получить Quaternion значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static Quaternion GetQuaternion(string key, Quaternion defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            float x = PlayerPrefs.GetFloat(key + ".x", defaultValue.x);
            float y = PlayerPrefs.GetFloat(key + ".y", defaultValue.y);
            float z = PlayerPrefs.GetFloat(key + ".z", defaultValue.z);
            float w = PlayerPrefs.GetFloat(key + ".w", defaultValue.w);

            Quaternion result = new Quaternion(x, y, z, w);
            return result;
        }

        /// <summary>
        /// Назначить Quaternion значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetQuaternion(string key, Quaternion value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            PlayerPrefs.SetFloat(key + ".x", value.x);
            PlayerPrefs.SetFloat(key + ".y", value.y);
            PlayerPrefs.SetFloat(key + ".z", value.z);
            PlayerPrefs.SetFloat(key + ".w", value.w);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Получить JSON значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public static T GetJson<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return defaultValue;
            }

            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                ServiceDebug.LogWarning($"JSON с ключом «{key}» не найден в файле сохранения");
                return defaultValue;
            }

            T result = JsonUtility.FromJson<T>(json);
            if (result == null)
            {
                return defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Назначить JSON значение
        /// </summary>
        /// <param name="key">Ключ записи</param>
        /// <param name="value">Новое значение</param>
        public static void SetJson<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                KeyNotFoundLog(key);
                return;
            }

            if (value == null)
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                return;
            }

            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
        
        private static void KeyNotFoundLog(string key) => ServiceDebug.LogWarning($"Ключ «{key}» не найден в PlayerPrefs");
    }
}
