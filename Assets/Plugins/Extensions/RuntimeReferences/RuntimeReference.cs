using System;
using UnityEngine;

namespace Extensions.RuntimeReferences
{
    /// <summary>
    /// Ассет-канал рантайм-ссылки: хранит ссылку на объект сцены/сервис, публикуемую в рантайме
    /// </summary>
    /// <remarks>
    /// Объекты сцены/префаба публикуют себя (<see cref="Set"/>) при готовности и снимают (<see cref="Clear"/>)
    /// при выключении/уничтожении, а динамически созданные потребители читают объект из этого ассета.
    /// Конкретный наследник закрывает <typeparamref name="T"/> и добавляет [CreateAssetMenu]
    /// </remarks>
    /// <typeparam name="T"> Тип публикуемого объекта (компонент или сервис) </typeparam>
    public abstract class RuntimeReference<T> : ScriptableObject where T : class
    {
        /// <summary> Объект опубликован </summary>
        public event Action<T> onInitialized;
        /// <summary> Объект снят </summary>
        public event Action onReleased;

        /// <summary> Текущий опубликованный объект (null, пока не опубликован) </summary>
        public T Current { get; private set; }
        /// <summary> Опубликован ли объект </summary>
        public bool HasValue => Current != null;

        /// <summary> Опубликовать объект (null трактуется как <see cref="Clear"/>) </summary>
        /// <param name="value">Публикуемый объект</param>
        public void Set(T value)
        {
            if (value is null)
            {
                Clear();
                return;
            }

            if (ReferenceEquals(Current, value)) return;

            Current = value;
            onInitialized?.Invoke(value);
        }

        /// <summary> Снять опубликованный объект </summary>
        public void Clear()
        {
            if (Current is null) return;

            Current = null;
            onReleased?.Invoke();
        }
    }
}
