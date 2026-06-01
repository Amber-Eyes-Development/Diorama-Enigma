using System;
using System.Collections.Generic;
using Extensions.Data;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Базовая абстракция ScriptableValue — хранилища значения
    /// </summary>
    public abstract class ScriptableValue<T> : BaseScriptableValue
    {
        /// <summary>
        /// Событие изменения значения
        /// </summary>
        public event Action<T> onValueChanged;

        /// <summary>
        /// Текущее значение
        /// </summary>
        public virtual T Value
        {
            get
            {
                LoadIfNeeded();
                return runtimeValue;
            }
            set => SetValue(value);
        }
        
        /// <summary>
        /// Является ли состояние глобальным (иначе состояние отдельно для каждого активного профиля)
        /// </summary>
        public bool IsGlobal => isGlobal;
        /// <summary>
        /// Значение по-умолчанию
        /// </summary>
        public T DefaultValue => defaultValue;

        [Header("Хранимое значение"), Space]
        [Tooltip("Сохранять ли значение между сессиями")]
        [SerializeField] protected bool isSaveable = false;
        [Tooltip("Глобальный профиль сохранения, иначе состояние отдельно для каждого активного профиля")]
        [SerializeField] protected bool isGlobal = false;
        [Tooltip("Дефолтное значение, используемое если нет сохранения или оно отключено")]
        [SerializeField] protected T defaultValue = default;
        
        [NonSerialized]
        protected T runtimeValue;
        protected string SaveProfile => isGlobal ? GLOBAL_PROFILE : null;
        protected bool isLoaded = false;

        protected virtual void OnEnable()
        {
            isLoaded = false;
            runtimeValue = defaultValue;
        }

        /// <summary>
        /// Установка значения
        /// </summary>
        public virtual void SetValue(T newValue)
        {
            LoadIfNeeded();

            if (EqualityComparer<T>.Default.Equals(runtimeValue, newValue))
                return;

            runtimeValue = newValue;

            onValueChanged?.Invoke(runtimeValue);

            if (Application.isPlaying && isSaveable)
            {
                JsonSaveLoad.Save(runtimeValue, Id, SaveProfile);
            }
        }
        
        /// <summary>
        /// Установить значение без уведомления подписчиков
        /// </summary>
        public void SetValueSilent(T newValue)
        {
            LoadIfNeeded();
            runtimeValue = newValue;
        }

        /// <summary>
        /// Уведомить подписчиков о текущем значении без его изменения
        /// </summary>
        public void ForceNotify() => onValueChanged?.Invoke(runtimeValue);

        /// <summary>
        /// Сброс значения к дефолтному
        /// </summary>
        public override void ResetToDefault() => SetValue(defaultValue);
        
        /// <summary>
        /// Сброс значения к дефолтному
        /// </summary>
        public override void Clear() => SetValue(default);
        
        protected virtual void LoadIfNeeded()
        {
            if (!Application.isPlaying || isLoaded) return;
            isLoaded = true;

            // Если значение не сохраняемое — всегда используем дефолт
            if (!isSaveable)
            {
                runtimeValue = defaultValue;
                return;
            }

            T loadedValue = JsonSaveLoad.Load(Id, defaultValue, SaveProfile);
            runtimeValue = loadedValue;
        }
    }
}
