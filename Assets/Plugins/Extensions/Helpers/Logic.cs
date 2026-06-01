using Extensions.Log;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Extensions.Helpers
{
    /// <summary>
    /// Базовая быстрая логика
    /// </summary>
    public static class Logic
    {
        #region Проверки
        
        /// <summary>
        /// Проверка объекта на существование
        /// </summary>
        /// <param name="obj">Проверяемый объект</param>
        /// <returns></returns>
        [HideInCallstack]
        public static bool IsNull(Object obj, string name = "")
        {
            if (obj == null)
            {
                ServiceDebug.LogError(name == "" ? "Объект = null" : $"{name} = null");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверка объекта на существование
        /// </summary>
        /// <param name="obj">Проверяемый объект</param>
        /// <returns></returns>
        [HideInCallstack]
        public static bool IsNotNull(Object obj, string name = "") => !IsNull(obj, name);
        
        #endregion
    }
}