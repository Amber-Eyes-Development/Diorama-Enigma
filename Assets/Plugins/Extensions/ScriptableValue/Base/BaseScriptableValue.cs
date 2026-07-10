using System;
using Extensions.Identification;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Базовая абстракция ScriptableValue без указания типа
    /// </summary>
    public abstract class BaseScriptableValue : IdentifiableObject
    {
        protected const string GLOBAL_PROFILE = "global values";

        /// <summary>
        /// Значение изменилось
        /// </summary>
        public event Action onChanged;

        /// <summary>
        /// Равно ли текущее значение дефолтному
        /// </summary>
        public abstract bool IsDefault { get; }

        /// <summary>
        /// Сброс значения к дефолтному
        /// </summary>
        public abstract void ResetToDefault();

        /// <summary>
        /// Очистка значения
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Оповестить об изменении значения (вызывается наследником при реальном изменении)
        /// </summary>
        protected void RaiseChanged() => onChanged?.Invoke();
    }
}