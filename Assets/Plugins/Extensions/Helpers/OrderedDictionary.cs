using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions.Helpers
{
    /// <summary>
    /// Обертка словаря с сохранением очередности добавления/редактирования
    /// </summary>
    /// <remarks>
    /// Последняя добавленная/редактированная запись помещается в конец
    /// </remarks>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    /// <typeparam name="TValue">Тип значения</typeparam>
    public class OrderedDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> dict = new();
        private readonly List<TKey> order = new();

        /// <summary>
        /// Получить или установить значение по ключу.
        /// При установке существующего ключа запись перемещается в конец.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                if (!dict.TryGetValue(key, out TValue value))
                    throw new KeyNotFoundException($"Запись с ключом '{key}' не найдена.");
                return value;
            }
            set
            {
                if (dict.ContainsKey(key))
                    order.Remove(key);
                dict[key] = value;
                order.Add(key);
            }
        }

        /// <summary>
        /// Количество записей
        /// </summary>
        public int Count => dict.Count;

        /// <summary>
        /// Значения в порядке добавления/редактирования
        /// </summary>
        public IEnumerable<TValue> Values => order.Select(k => dict[k]);

        /// <summary>
        /// Ключи в порядке добавления/редактирования
        /// </summary>
        public IEnumerable<TKey> Keys => order.AsEnumerable();

        /// <summary>
        /// Добавить новую запись
        /// </summary>
        /// <exception cref="ArgumentException">Если запись с таким ключом уже существует</exception>
        public void Add(TKey key, TValue value)
        {
            if (!dict.TryAdd(key, value))
                throw new ArgumentException($"Запись с ключом '{key}' уже существует.");
            order.Add(key);
        }

        /// <summary>
        /// Попробовать получить значение по ключу
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);

        /// <summary>
        /// Проверить наличие записи по ключу
        /// </summary>
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);

        /// <summary>
        /// Очистить все записи
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            order.Clear();
        }

        /// <summary>
        /// Получить последнее добавленное/редактированное значение
        /// </summary>
        /// <exception cref="InvalidOperationException">Если словарь пуст</exception>
        public TValue Last()
        {
            if (order.Count == 0)
                throw new InvalidOperationException("Словарь пуст.");
            return dict[order[^1]];
        }

        /// <summary>
        /// Попробовать получить последнее добавленное/редактированное значение
        /// </summary>
        public bool TryGetLast(out TValue value)
        {
            if (order.Count == 0)
            {
                value = default;
                return false;
            }
            value = dict[order[^1]];
            return true;
        }

        /// <summary>
        /// Попробовать получить предпоследнее добавленное/редактированное значение
        /// </summary>
        public bool TryGetPrevious(out TValue value)
        {
            if (order.Count < 2)
            {
                value = default;
                return false;
            }
            value = dict[order[^2]];
            return true;
        }

        /// <summary>
        /// Конвертировать в обычный словарь
        /// </summary>
        /// <remarks>Порядок не гарантируется</remarks>
        public Dictionary<TKey, TValue> ToDictionary() => new(dict);
    }
}