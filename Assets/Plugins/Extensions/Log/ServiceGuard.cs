using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Extensions.Log
{
    /// <summary>
    /// Обёртка над ServiceDebug — логирует ошибку и бросает исключение одновременно
    /// </summary>
    public static class ServiceGuard
    {
        /// <summary>
        /// Бросает <see cref="ArgumentNullException"/> если значение null
        /// </summary>
        [HideInCallstack]
        public static void NotNull<T>(
            T value,
            string paramName,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "") where T : class
        {
            if (value != null) return;
            Throw(new ArgumentNullException(nameof(paramName), $"'{paramName}' не должен быть null."), filePath, memberName);
        }

        /// <summary>
        /// Бросает <see cref="ArgumentException"/> если строка null или пустая
        /// </summary>
        [HideInCallstack]
        public static void NotNullOrEmpty(
            string value,
            string paramName,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            if (!string.IsNullOrEmpty(value)) return;
            Throw(new ArgumentException($"'{paramName}' не должен быть null или пустым.", paramName), filePath, memberName);
        }

        /// <summary>
        /// Бросает <see cref="InvalidOperationException"/> если условие ложно.
        /// Используется для проверки состояния объекта, а не аргументов.
        /// </summary>
        [HideInCallstack]
        public static void IsTrue(
            bool condition,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            if (condition) return;
            Throw(new InvalidOperationException(message), filePath, memberName);
        }

        /// <summary>
        /// Бросает <see cref="ArgumentOutOfRangeException"/> если значение вне диапазона [min, max]
        /// </summary>
        [HideInCallstack]
        public static void InRange(
            float value,
            float min,
            float max,
            string paramName,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            if (value >= min && value <= max) return;
            Throw(new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' = {value} вне диапазона [{min}, {max}]."), filePath, memberName);
        }

        [HideInCallstack]
        private static void Throw(Exception exception, string filePath, string memberName)
        {
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            ServiceDebug.LogError($"[{className}.{memberName}] {exception.Message}");
            throw exception;
        }
    }
}