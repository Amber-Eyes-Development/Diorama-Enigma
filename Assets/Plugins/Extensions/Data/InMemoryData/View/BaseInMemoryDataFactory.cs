using System;
using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Абстракция UI фабрики
    /// </summary>
     /// <typeparam name="TPrefab">Тип префаба для спавна</typeparam>
     /// <typeparam name="TEntry">Единица данных, для которой спавнится префаб</typeparam>
     /// <typeparam name="TContainer">Контейнер единиц данных</typeparam>
    public abstract class BaseInMemoryDataFactory<TPrefab, TEntry, TContainer> : BaseInMemoryDataFactory<TPrefab>  
        where TPrefab : MonoBehaviour
        where TEntry : InMemoryDataEntry
        where TContainer : InMemoryDataContainer<TEntry>
    {
        [Header("Данные"), Space]
        [SerializeField]
        protected TContainer dataContainer;
        
        [Header("UI элемент"), Space]
        [SerializeField]
        [Tooltip("Префаб выводимого элемента")]
        protected TPrefab itemElementPrefab;
        [SerializeField]
        [Tooltip("Корень заспавненных элементов")]
        protected Transform elementsRoot;

        [Header("Опции заселения"), Space]
        [SerializeField]
        protected bool rebuildOnStart = true;
        [SerializeField]
        protected bool rebuildOnEnable = true;
        [SerializeField]
        protected bool rebuildOnInventoryDataUpdated = true;

        #region MonoBehavour
        
        protected virtual void Start()
        {
            if (rebuildOnStart) Rebuild();
        }
        
        protected virtual void OnEnable()
        {
            if (dataContainer != null && rebuildOnInventoryDataUpdated) dataContainer.onDataUpdated += Rebuild;
            
            if (rebuildOnEnable) Rebuild();
        }
        
        protected virtual void OnDisable()
        {
            if (dataContainer != null && rebuildOnInventoryDataUpdated) dataContainer.onDataUpdated -= Rebuild;
        }
        
        #endregion
        
        /// <summary>
        /// Популяция фабрикой
        /// </summary>
        public virtual void Rebuild()
        {
            if (dataContainer == null || itemElementPrefab == null)
            {
                ServiceDebug.LogError($"{name}: не все ссылки заполнены");
                return;
            }

            Clear();
            Populate(dataContainer.Data);
        }
        
        #region Основные операции фабрики
        
        /// <summary>
        /// Удалить все элементы из корня заселения
        /// </summary>
        protected void Clear()
        {
            if (elementsRoot == null)
            {
                ServiceDebug.LogError($"{name}: ссылка на корень элементов UI не заполнена");
                return;
            }

            for (int i = elementsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(elementsRoot.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Создать инстансы префаба по данным контейнера в корне заселения
        /// </summary>
        /// <param name="data"></param>
        protected void Populate(List<TEntry> data)
        {
            if (data == null)
            {
                ServiceDebug.LogError($"{name}: данные отсутствуют");
                return;
            }

            foreach (TEntry item in data)
            {
                if (item == null) continue;
                if (OnValidateItem(item)) continue;

                TPrefab instance = Instantiate(itemElementPrefab, elementsRoot);
                if (instance != null) OnInstanceInitialization(instance, item, dataContainer);
                OnObjectInstantiatedEvent(instance);
            }
        }
        
        #endregion
        
        #region Опциональные операции во время создания инстанса при популяции
        
        /// <summary>
        /// Опциональное валидирование данных — условие создания инстанса
        /// </summary>
        /// <param name="item">Запись контейнера</param>
        /// <returns>Если возвращает true — создается инстанс префаба</returns>
        protected virtual bool OnValidateItem(TEntry item) => false;

        /// <summary>
        /// Опциональная инициализация инстанса
        /// </summary>
        /// <param name="instance">Созданный инстанс</param>
        /// <param name="item">Запись контейнера</param>
        /// <param name="container">Контейнер</param>
        protected virtual void OnInstanceInitialization(TPrefab instance, TEntry item, TContainer container) { }
        
        #endregion
    }

    /// <summary>
    /// Базовая UI фабрика
    /// </summary>
    /// <typeparam name="TPrefab">Тип префаба для спавна</typeparam>
    public class BaseInMemoryDataFactory<TPrefab> : MonoBehaviour
        where TPrefab : MonoBehaviour
    {
        /// <summary>
        /// Событие спавна объекта фабрики
        /// </summary>
        public event Action<TPrefab> onObjectInstantiated;
        
        protected void OnObjectInstantiatedEvent(TPrefab instance) => onObjectInstantiated?.Invoke(instance);
    }
}