using System;
using System.Collections.Generic;

namespace Extensions.Reactive
{
    /// <summary>
    /// Реактивное свойство
    /// </summary>
    /// <remarks>
    /// Для отписки вызывать либо Unsubscribe с явным указанием слушателя, либо через ReactiveSubscription.Dispose
    /// </remarks>
    public class ReactiveProperty<T>
    {
        private T _value;

        private bool isNotifying;
        private readonly List<Action<T>> simpleSubscribers = new();
        private readonly List<Action<T, T>> diffSubscribers = new();
        private readonly List<Action<T>> simpleSubscribersToRemove = new();
        private readonly List<Action<T, T>> diffSubscribersToRemove = new();
        
        private readonly IEqualityComparer<T> comparer;
        private readonly Func<T, T> normalizer;

        private ReadOnlyReactiveProperty<T> readOnlyWrapper;

        /// <summary>
        /// Текущее значение
        /// </summary>
        public T Value
        {
            get => _value;
            set => SetValue(value);
        }

        /// <summary>
        /// Есть ли подписчики
        /// </summary>
        public bool HasSubscribers =>
            simpleSubscribers.Count > 0 ||
            diffSubscribers.Count > 0;

        #region Конструкторы
        
        /// <summary>
        /// Новое реактивное свойство
        /// </summary>
        public ReactiveProperty() => comparer = EqualityComparer<T>.Default;

        /// <summary>
        /// Новое реактивное свойство
        /// </summary>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        /// <param name="normalizer">Нормализатор значения</param>
        public ReactiveProperty(T defaultValue, Func<T, T> normalizer = null)
        {
            this.normalizer = normalizer;
            _value = normalizer != null ? normalizer(defaultValue) : defaultValue;
            comparer = EqualityComparer<T>.Default;
        }

        /// <summary>
        /// Новое реактивное свойство
        /// </summary>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        /// <param name="comparer">Кастомный сравниватель</param>
        /// <remarks>
        /// Сравниватель по-умолчанию не отправляет события, если знаечние не изменено.
        /// Кастомный может переопределить логику сравнения.
        /// </remarks>
        public ReactiveProperty(T defaultValue, IEqualityComparer<T> comparer, Func<T, T> normalizer = null)
        {
            this.normalizer = normalizer;
            _value = normalizer != null ? normalizer(defaultValue) : defaultValue;
            this.comparer = comparer ?? EqualityComparer<T>.Default;
        }
        
        #endregion

        #region Подписка и отписка
      
        /// <summary>
        /// Подписка на изменение значения
        /// </summary>
        /// <param name="callback">Событие</param>
        /// <param name="notifyImmediately">Оповестить после подписки</param>
        /// <returns>Подписка на реактивный источник</returns>
        public ReactiveSubscription Subscribe(Action<T> callback, bool notifyImmediately = false)
        {
            if (!simpleSubscribers.Contains(callback))
                simpleSubscribers.Add(callback);

            if (notifyImmediately)
                callback(_value);

            return new ReactiveSubscription(() => Unsubscribe(callback));
        }
        
        /// <summary>
        /// Подписка с получением предыдущего и нового значения (diff)
        /// </summary>
        /// <param name="callback">Событие</param>
        /// <param name="notifyImmediately">Оповестить после подписки</param>
        /// <returns>Подписка на реактивный источник</returns>
        public ReactiveSubscription Subscribe(Action<T, T> callback, bool notifyImmediately = false)
        {
            if (!diffSubscribers.Contains(callback))
                diffSubscribers.Add(callback);

            if (notifyImmediately)
                callback(_value, _value);

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
            if (isNotifying)
            {
                if (!simpleSubscribersToRemove.Contains(callback))
                    simpleSubscribersToRemove.Add(callback);

                return;
            }

            simpleSubscribers.Remove(callback);
        }

        
        /// <summary>
        /// Альтернативный способ отписки (с diff) без хранения ReactiveSubscription
        /// </summary>
        /// <remarks>
        /// Не смешивать с Dispose() одной и той же подписки
        /// </remarks>
        /// <param name="callback">Событие</param>
        public void Unsubscribe(Action<T, T> callback)
        {
            if (isNotifying)
            {
                if (!diffSubscribersToRemove.Contains(callback))
                    diffSubscribersToRemove.Add(callback);

                return;
            }

            diffSubscribers.Remove(callback);
        }

        /// <summary>
        /// Отписать всех слушателей
        /// </summary>
        public void ClearSubscribers()
        {
            simpleSubscribers.Clear();
            diffSubscribers.Clear();
        }
        
        #endregion

        #region Вспомогательные методы
        
        /// <summary>
        /// Установить значение
        /// </summary>
        /// <param name="newValue">Новое значение</param>
        /// <param name="forceNotify">Форсированное оповещение слушателей</param>
        /// <returns>true если слушатели оповещены, иначе false</returns>
        public bool SetValue(T newValue, bool forceNotify = false)
        {
            if (normalizer != null) newValue = normalizer(newValue);

            if (!forceNotify && comparer.Equals(_value, newValue)) return false;

            T previous = _value;
            _value = newValue;

            NotifyInternal(previous, newValue);
            return true;
        }

        /// <summary>
        /// Уведомить подписчиков
        /// </summary>
        /// <remarks>
        /// Для оповещения если значение не изменено, например для mutable значений (добавление в List/Dictionary)
        /// </remarks>
        public void Notify() => NotifyInternal(_value, _value);

        /// <summary>
        /// Получить read-only обертку
        /// </summary>
        public ReadOnlyReactiveProperty<T> AsReadOnly()
        {
            readOnlyWrapper ??= new ReadOnlyReactiveProperty<T>(this);
            return readOnlyWrapper;
        }
        
        #endregion

        private void NotifyInternal(T previous, T current)
        {
            isNotifying = true;

            foreach (var t in simpleSubscribers)
                t?.Invoke(current);

            foreach (var t in diffSubscribers)
                t?.Invoke(previous, current);

            isNotifying = false;

            if (simpleSubscribersToRemove.Count > 0)
            {
                foreach (var t in simpleSubscribersToRemove)
                    simpleSubscribers.Remove(t);

                simpleSubscribersToRemove.Clear();
            }

            if (diffSubscribersToRemove.Count > 0)
            {
                foreach (var t in diffSubscribersToRemove)
                    diffSubscribers.Remove(t);

                diffSubscribersToRemove.Clear();
            }
        }
    }
}