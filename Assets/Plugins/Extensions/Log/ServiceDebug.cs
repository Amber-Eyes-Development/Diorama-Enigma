using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Extensions.Log
{
    /// <summary>
    /// Служебные дебаги
    /// </summary>
    public static class ServiceDebug
    {
        private const string MESSAGE_POSTFIX = "\n";
        
        public static bool EnableInfo = true;
        public static bool EnableWarnings = true;
        public static bool EnableErrors = true;

        private const string ColorInfo = "#00AEEF";
        private const string ColorWarning = "#FFB300";
        private const string ColorError = "#FF3B30";
        
        #region TypeContext
        
        /// <summary>
        /// Информационный лог с контекстом типа
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <typeparam name="T">Тип</typeparam>
        [HideInCallstack]
        public static void Log<T>(string message)
        {
            if (!EnableInfo) return;
            Debug.Log(Format(typeof(T).Name, message + MESSAGE_POSTFIX, ColorInfo));
        }

        /// <summary>
        /// Предупредительный лог с контекстом типа
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <typeparam name="T">Тип</typeparam>
        [HideInCallstack]
        public static void LogWarning<T>(string message)
        {
            if (!EnableWarnings) return;
            Debug.LogWarning(Format(typeof(T).Name, message + MESSAGE_POSTFIX, ColorWarning));
        }
        
        /// <summary>
        /// Лог ошибки с контекстом типа
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <typeparam name="T">Тип</typeparam>
        [HideInCallstack]
        public static void LogError<T>(string message)
        {
            if (!EnableErrors) return;
            Debug.LogError(Format(typeof(T).Name, message + MESSAGE_POSTFIX, ColorError));
        }
        
        #endregion

        #region ObjectContext

        /// <summary>
        /// Информационный лог с контекстом объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="message">Сообщение</param>
        [HideInCallstack]
        public static void Log(Object obj, string message)
        {
            if (!EnableInfo) return;
            Debug.Log(Format(obj ? obj.name : "NULL", message + MESSAGE_POSTFIX, ColorInfo), obj);
        }

        /// <summary>
        /// Предупредительный лог с контекстом объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="message">Сообщение</param>
        [HideInCallstack]
        public static void LogWarning(Object obj, string message)
        {
            if (!EnableWarnings) return;
            Debug.LogWarning(Format(obj ? obj.name : "NULL", message + MESSAGE_POSTFIX, ColorWarning), obj);
        }

        /// <summary>
        /// Лог ошибки с контекстом объекта
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="message">Сообщение</param>
        [HideInCallstack]
        public static void LogError(Object obj, string message)
        {
            if (!EnableErrors) return;
            Debug.LogError(Format(obj ? obj.name : "NULL", message + MESSAGE_POSTFIX, ColorError), obj);
        }
        
        #endregion

        #region AutoContext
        
        /// <summary>
        /// Информационный лог с автоматическим контекстом
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="filePath">Автоматический путь до скрипта</param>
        [HideInCallstack]
        public static void Log(string message, [CallerFilePath] string filePath = "")
        {
            if (!EnableInfo) return;
            Debug.Log(Format(GetClassName(filePath), message + MESSAGE_POSTFIX));
        }

        /// <summary>
        /// Предупредительный лог с автоматическим контекстом
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="filePath">Автоматический путь до скрипта</param>
        [HideInCallstack]
        public static void LogWarning(string message, [CallerFilePath] string filePath = "")
        {
            if (!EnableWarnings) return;
            Debug.LogWarning(Format(GetClassName(filePath), message + MESSAGE_POSTFIX));
        }

        /// <summary>
        /// Лог ошибки с автоматическим контекстом
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="filePath">Автоматический путь до скрипта</param>
        [HideInCallstack]
        public static void LogError(string message, [CallerFilePath] string filePath = "")
        {
            if (!EnableErrors) return;
            Debug.LogError(Format(GetClassName(filePath), message + MESSAGE_POSTFIX));
        }

        private static string GetClassName(string filePath) => Path.GetFileNameWithoutExtension(filePath);
        
        #endregion

        #region Format

        private static string Format(string context, string message, string color) => $"<b><color={color}>[{context}]</color></b> {message}";
        
        private static string Format(string context, string msg)
        {
            string color = GetColorForContext(context);
            return $"<b><color={color}>[{context}]</color></b> {msg}";
        }

        private static readonly string[] Colors =
        {
            "#00AEEF", // голубой
            "#FFB300", // янтарный
            "#FF3B30", // красный
            "#6DD400", // зелёный
            "#A100FF", // фиолетовый
            "#FF6A00", // оранжево-красный
            "#34C759", // светло-зелёный
            "#5AC8FA", // голубой пастельный
        };

        private static string GetColorForContext(string context)
        {
            if (string.IsNullOrEmpty(context))
                return "#FFFFFF";

            int hash = context.GetHashCode();
            int index = Mathf.Abs(hash) % Colors.Length;

            return Colors[index];
        }
        
        #endregion
    }
}
