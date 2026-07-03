using Extensions.Log;
using UnityEngine;

namespace Extensions.RuntimeReferences
{
    /// <summary>
    /// База потребителя: держит канал <see cref="RuntimeReference{T}"/> и реагирует на появление/снятие объекта
    /// </summary>
    /// <remarks>
    /// Наследоваться, если у скрипта нет другого родителя. Если родитель занят — держать
    /// <see cref="RuntimeReference{T}"/> полем и самостоятельно читать <see cref="RuntimeReference{T}.Current"/>
    /// </remarks>
    /// <typeparam name="T"> Тип потребляемого объекта </typeparam>
    public abstract class RuntimeReferenceConsumer<T> : MonoBehaviour where T : class
    {
        /// <summary> Текущий объект (null, пока не доставлен) </summary>
        protected T Value { get; private set; }

        [Tooltip("Канал, из которого берётся объект")]
        [SerializeField] private RuntimeReference<T> reference;

        protected virtual void OnEnable()
        {
            if (reference == null)
            {
                ServiceDebug.LogError($"{nameof(reference)} не назначен");
                return;
            }

            reference.onInitialized += Receive;
            reference.onReleased += Release;

            if (reference.HasValue) Receive(reference.Current);
        }

        protected virtual void OnDisable()
        {
            if (reference != null)
            {
                reference.onInitialized -= Receive;
                reference.onReleased -= Release;
            }

            Release();
        }

        private void Receive(T value)
        {
            if (ReferenceEquals(Value, value)) return;

            Value = value;
            OnInitialized(value);
        }

        private void Release()
        {
            if (Value is null) return;

            Value = null;
            OnReleased();
        }

        /// <summary> Объект появился (или уже был на момент включения) </summary>
        /// <param name="value"> Доставленный объект </param>
        protected abstract void OnInitialized(T value);

        /// <summary> Объект снят или скрипт выключается </summary>
        protected virtual void OnReleased() { }
    }
}
