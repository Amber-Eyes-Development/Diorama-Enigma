using System;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Generics
{
    /// <summary>
    /// Инициализируемый MonoBehaviour
    /// </summary>
    public class InitializableMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Событие инициализации
        /// </summary>
        public event Action onInitialized;
        
        /// <summary>
        /// Состояние инициализации
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        private bool _isInitialized = false;

        /// <summary>
        /// Инициализация
        /// </summary>
        public virtual void Initialize()
        {
            _isInitialized = true;
            onInitialized?.Invoke();
        }
        
        /// <summary>
        /// Инициализация
        /// </summary>
        public virtual void Initialize(bool initialize)
        {
            _isInitialized = initialize;
            if (_isInitialized) onInitialized?.Invoke();
            NotifyInitialized();
        }
        
        /// <summary>
        /// Оповещение о состоянии инициализации
        /// </summary>
        [HideInCallstack]
        public virtual void NotifyInitialized()
        {
            if (!IsInitialized) ServiceDebug.LogError("Скрипт не инициализирован");
        }
    }
}