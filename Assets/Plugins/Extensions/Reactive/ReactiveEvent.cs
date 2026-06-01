using System;
using System.Collections.Generic;

namespace Extensions.Reactive
{
    /// <summary>
    /// Реактивное событие без параметров
    /// </summary>
    public class ReactiveEvent
    {
        private bool isInvoking;
        private readonly List<Action> subscribers = new();
        private readonly List<Action> subscribersToRemove = new();
        
        /// <summary>
        /// Подписка на событие
        /// </summary>
        /// <param name="callback">Событие</param>
        /// <returns>Подписка на реактивный источник</returns>
        public ReactiveSubscription Subscribe(Action callback)
        {
            if (subscribers.Contains(callback))
                return new ReactiveSubscription(() => Unsubscribe(callback));

            subscribers.Add(callback);

            return new ReactiveSubscription(() => Unsubscribe(callback));
        }
        
        /// <summary>
        /// Альтернативный способ отписки без хранения ReactiveSubscription
        /// </summary>
        /// <remarks>
        /// Не смешивать с Dispose() одной и той же подписки
        /// </remarks>
        /// <param name="callback">Событие</param>
        public void Unsubscribe(Action callback)
        {
            if (isInvoking)
            {
                if (!subscribersToRemove.Contains(callback))
                    subscribersToRemove.Add(callback);

                return;
            }

            subscribers.Remove(callback);
        }
        
        /// <summary>
        /// Вызвать событие
        /// </summary>
        public void Invoke()
        {
            if (subscribers.Count == 0) return;

            isInvoking = true;
            foreach (var t in subscribers)
                t?.Invoke();
            isInvoking = false;

            if (subscribersToRemove.Count <= 0) return;
            
            foreach (var t in subscribersToRemove)
                subscribers.Remove(t);
            subscribersToRemove.Clear();
        }

        /// <summary>
        /// Отписать всех слушателей
        /// </summary>
        public void ClearSubscribers() => subscribers.Clear();
    }

    /// <summary>
    /// Реактивное событие с параметром
    /// </summary>
    /// <typeparam name="T">Тип параметра</typeparam>
    public class ReactiveEvent<T>
    {
        private bool isInvoking;
        private readonly List<Action<T>> subscribers = new();
        private readonly List<Action<T>> subscribersToRemove = new();
        
        /// <summary>
        /// Подписка на событие
        /// </summary>
        /// <param name="callback">Событие</param>
        /// <returns>Подписка на реактивный источник</returns>
        public ReactiveSubscription Subscribe(Action<T> callback)
        {
            if (subscribers.Contains(callback))
                return new ReactiveSubscription(() => Unsubscribe(callback));

            subscribers.Add(callback);

            return new ReactiveSubscription(() => Unsubscribe(callback));
        }
        
        /// <summary>
        /// Альтернативный способ отписки без хранения ReactiveSubscription
        /// </summary>
        /// <remarks>
        /// Не смешивать с Dispose() одной и той же подписки
        /// </remarks>
        /// <param name="callback">Событие</param>
        public void Unsubscribe(Action<T> callback)
        {
            if (isInvoking)
            {
                if (!subscribersToRemove.Contains(callback))
                    subscribersToRemove.Add(callback);

                return;
            }

            subscribers.Remove(callback);
        }
        
        /// <summary>
        /// Вызвать событие
        /// </summary>
        /// <param name="value">Значение параметра</param>
        public void Invoke(T value)
        {
            if (subscribers.Count == 0) return;

            isInvoking = true;
            foreach (var t in subscribers)
                t?.Invoke(value);
            isInvoking = false;

            if (subscribersToRemove.Count <= 0) return;
            
            foreach (var t in subscribersToRemove)
                subscribers.Remove(t);
            subscribersToRemove.Clear();
        }

        /// <summary>
        /// Отписать всех слушателей
        /// </summary>
        public void ClearSubscribers() => subscribers.Clear();
    }
}