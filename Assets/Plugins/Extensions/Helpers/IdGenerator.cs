using System;

namespace Extensions.Helpers
{
    /// <summary>
    /// Генератор уникальных идентификаторов
    /// </summary>
    public static class IdGenerator
    {
        private const string FORMAT = "N";
        
        /// <summary>
        /// Сгенерировать глобально уникальный идентификатор
        /// </summary>
        public static string NewGuid()
        {
            return Guid.NewGuid().ToString(FORMAT);
        }

        /// <summary>
        /// Сгенерировать короткий идентификатор
        /// </summary>
        public static string NewShort()
        {
            return Guid.NewGuid().ToString(FORMAT)[..8];
        }

        /// <summary>
        /// Сгенерировать идентификатор с префиксом
        /// </summary>
        public static string NewWithPrefix(string prefix)
        {
            return $"{prefix}_{NewShort()}";
        }
    }
}