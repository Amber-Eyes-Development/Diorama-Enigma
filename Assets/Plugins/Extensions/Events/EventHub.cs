using System;
using System.Collections.Generic;

namespace Extensions.Events
{
    /// <summary>
    /// Локальный хаб событий
    /// </summary>
    /// <remarks>
    /// Событие TEvent — структура передаваемых данных.
    /// Вызываемое событие должно иметь параметр того же типа.
    /// </remarks>
    public sealed class EventHub
    {
        private readonly Dictionary<Type, Delegate> handlers = new();
        private readonly Dictionary<Type, object> replayEvents = new();

        /// <summary>
        /// Подписаться на событие
        /// </summary>
        /// <param name="handler">Исполняемое действие</param>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <exception cref="ArgumentNullException">Действие не назначено</exception>
        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            Type eventType = typeof(TEvent);

            if (handlers.TryGetValue(eventType, out Delegate existing))
            {
                // Проверить что такого обработчика ещё нет
                foreach (Delegate d in existing.GetInvocationList())
                    if (d.Equals(handler)) return;

                handlers[eventType] = Delegate.Combine(existing, handler);
                return;
            }

            handlers[eventType] = handler;
        }

        /// <summary>
        /// Отписаться от события
        /// </summary>
        /// <param name="handler">Исполняемое действие</param>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <exception cref="ArgumentNullException">Действие не указано</exception>
        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            Type eventType = typeof(TEvent);

            if (!handlers.TryGetValue(eventType, out Delegate existing)) return;

            Delegate updated = Delegate.Remove(existing, handler);

            if (updated == null)
            {
                handlers.Remove(eventType);
                return;
            }

            handlers[eventType] = updated;
        }
        
        /// <summary>
        /// Опубликовать событие
        /// </summary>
        /// <param name="evt">Событие</param>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <exception cref="InvalidOperationException">Колбэк невалиден</exception>
        public void Publish<TEvent>(TEvent evt)
        {
            Type eventType = typeof(TEvent);
            if (!handlers.TryGetValue(eventType, out Delegate existing)) return;

            Action<TEvent> callback = existing as Action<TEvent>;
            if (callback == null)
                throw new InvalidOperationException($"Невалидный коллбек (несовпадение типов) для события '{eventType.Name}'");

            // Снимаем копию на случай если обработчик изнутри вызовет Unsubscribe
            Action<TEvent> snapshot = (Action<TEvent>)callback.Clone();
            snapshot.Invoke(evt);
        }

        #region Replay events

        /// <summary>
        /// Подписаться на событие и сразу получить последнее опубликованное replay-событие, если оно есть
        /// </summary>
        /// <param name="handler">Исполняемое действие</param>
        /// <typeparam name="TEvent">Тип события</typeparam>
        public void SubscribeReplay<TEvent>(Action<TEvent> handler)
        {
            Subscribe(handler);

            if (replayEvents.TryGetValue(typeof(TEvent), out object storedEvent))
            {
                handler.Invoke((TEvent)storedEvent);
            }
        }
        
        /// <summary>
        /// Опубликовать событие и сохранить его как последнее replay-событие этого типа
        /// </summary>
        /// <param name="evt">Событие</param>
        /// <typeparam name="TEvent">Тип события</typeparam>
        public void PublishReplay<TEvent>(TEvent evt)
        {
            replayEvents[typeof(TEvent)] = evt;
            Publish(evt);
        }

        /// <summary>
        /// Попробовать получить последнее replay-событие указанного типа
        /// </summary>
        /// <param name="evt">Последнее событие</param>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <returns>true если событие есть, иначе false</returns>
        public bool TryGetLastEvent<TEvent>(out TEvent evt)
        {
            if (replayEvents.TryGetValue(typeof(TEvent), out object storedEvent))
            {
                evt = (TEvent)storedEvent;
                return true;
            }

            evt = default;
            return false;
        }

        /// <summary>
        /// Очистить последнее replay-событие указанного типа
        /// </summary>
        /// <typeparam name="TEvent">Тип события</typeparam>
        public void ClearReplay<TEvent>() => replayEvents.Remove(typeof(TEvent));
        
        #endregion

        /// <summary>
        /// Очистить все подписки
        /// </summary>
        public void Clear()
        {
            handlers.Clear();
            replayEvents.Clear();
        }

        /// <summary>
        /// Есть ли подписчики
        /// </summary>
        /// <typeparam name="TEvent">Тип события</typeparam>
        /// <returns>true если есть, иначе false</returns>
        public bool HasSubscribers<TEvent>() => handlers.ContainsKey(typeof(TEvent));
    }
}