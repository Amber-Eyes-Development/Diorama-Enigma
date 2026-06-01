using System;
using System.Text;
using Extensions.Log;

namespace Extensions.Data
{
    /// <summary>
    /// Тип шифрования данных
    /// </summary>
    public enum DataEncryptionMode
    {
        None = 0,
        Xor = 1
    }

    /// <summary>
    /// Шифратор/дешифратор данных
    /// </summary>
    public static class DataEncryptor
    {
        /// <summary>
        /// Активный тип шифрования
        /// </summary>
        public static DataEncryptionMode EncryptionMode = DataEncryptionMode.None;
        
        private const string DEFAULT_KEY = "encryption_key";

        /// <summary>
        /// Шифрует строку в зависимости от выбранного режима, используя ключ по умолчанию
        /// </summary>
        public static string Encrypt(string plainText, DataEncryptionMode mode)
        {
            return Encrypt(plainText, mode, DEFAULT_KEY);
        }

        /// <summary>
        /// Расшифровывает строку в зависимости от выбранного режима, используя ключ по умолчанию
        /// </summary>
        public static string Decrypt(string cipherText, DataEncryptionMode mode)
        {
            return Decrypt(cipherText, mode, DEFAULT_KEY);
        }

        /// <summary>
        /// Шифрует строку в зависимости от выбранного режима и указанного ключа
        /// </summary>
        public static string Encrypt(string plainText, DataEncryptionMode mode, string key)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            if (mode == DataEncryptionMode.None)
            {
                return plainText;
            }

            if (mode == DataEncryptionMode.Xor)
            {
                return EncryptXorToBase64(plainText, key);
            }

            return plainText;
        }

        /// <summary>
        /// Расшифровывает строку в зависимости от выбранного режима и указанного ключа
        /// </summary>
        public static string Decrypt(string cipherText, DataEncryptionMode mode, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return string.Empty;
            }

            if (mode == DataEncryptionMode.None)
            {
                return cipherText;
            }

            if (mode == DataEncryptionMode.Xor)
            {
                return DecryptXorFromBase64(cipherText, key);
            }

            return cipherText;
        }

        /// <summary>
        /// Шифрует строку с помощью XOR и кодирует результат в Base64
        /// </summary>
        public static string EncryptXorToBase64(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key))
            {
                ServiceDebug.LogError("Задан невалидный ключ шифрования");
                throw new ArgumentException("Ключ должен не должен быть пуст", nameof(key));
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            byte[] encryptedBytes = XorBytes(dataBytes, keyBytes);

            string base64 = Convert.ToBase64String(encryptedBytes);
            return base64;
        }

        /// <summary>
        /// Дешифрует строку, зашифрованную через EncryptXorToBase64.
        /// </summary>
        public static string DecryptXorFromBase64(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key))
            {
                ServiceDebug.LogError("Задан невалидный ключ шифрования");
                throw new ArgumentException("Ключ должен не должен быть пуст", nameof(key));
            }

            byte[] encryptedBytes;

            try
            {
                encryptedBytes = Convert.FromBase64String(cipherText);
            }
            catch (FormatException)
            {
                return string.Empty;
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            byte[] decryptedBytes = XorBytes(encryptedBytes, keyBytes);

            string plainText = Encoding.UTF8.GetString(decryptedBytes);
            return plainText;
        }

        /// <summary>
        /// Применяет XOR к данным, используя повторяющийся ключ
        /// </summary>
        private static byte[] XorBytes(byte[] data, byte[] key)
        {
            int length = data.Length;
            int keyLength = key.Length;

            byte[] result = new byte[length];

            for (int i = 0; i < length; i++)
            {
                int keyIndex = i % keyLength;
                byte value = (byte)(data[i] ^ key[keyIndex]);
                result[i] = value;
            }

            return result;
        }
    }
}
