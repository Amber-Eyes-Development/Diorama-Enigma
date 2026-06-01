using System;
using System.Collections.Generic;
using Extensions.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Базовый оркестратор очередности действий кнопок <see cref="BaseAbstractButtonAction"/>
    /// </summary>
    [RequireComponent(typeof(Button))]
    public abstract class BaseButtonOrchestrator : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое после клика кнопки
        /// </summary>
        public event Action onButtonClicked;

        protected Button button;
        protected readonly List<BaseAbstractButtonAction> actions = new();
        
        /// <summary>
        /// Добавить действие для выполнения по нажатию
        /// </summary>
        public void AddAction(BaseAbstractButtonAction action)
        {
            if (action != null) actions.Add(action);
            sortActionsByPriority();
        }

        /// <summary>
        /// Удалить действие для выполнения по нажатию
        /// </summary>
        public void RemoveAction(BaseAbstractButtonAction action)
        {
            if (action != null) actions.Remove(action);
        }

        protected virtual void ProcessActions()
        {
            List<BaseAbstractButtonAction> processingActions = CollectionCopy.ListShallow(actions);
            foreach (var action in processingActions) action.OnButtonClickAction();
            onButtonClicked?.Invoke();
        }

        protected void sortActionsByPriority()
        {
            actions.Sort((a, b) => b.GetPriority.CompareTo(a.GetPriority));
        }
    }
}