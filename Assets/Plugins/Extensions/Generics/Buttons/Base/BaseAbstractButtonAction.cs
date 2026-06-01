using UnityEngine;

namespace Extensions.Generics
{
    /// <summary>
    /// Базовая абстракция кнопки
    /// </summary>
    public abstract class BaseAbstractButtonAction : MonoBehaviour
    {
        protected const int DEFAULT_PRIORITY = 1;

        /// <summary>
        /// Приоритет выполнения действия
        /// </summary>
        /// <returns></returns>
        public virtual int GetPriority => DEFAULT_PRIORITY;
        
        /// <summary>
        /// Нажатие на кнопку (зажатие выполнено)
        /// </summary>
        public abstract void OnButtonClickAction();
    }
}