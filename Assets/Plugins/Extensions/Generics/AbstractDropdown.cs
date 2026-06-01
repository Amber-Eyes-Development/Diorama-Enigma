using System;
using TMPro;
using UnityEngine;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция выпадающего списка <see cref="TMP_Dropdown"/>
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public abstract class AbstractDropdown : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое после изменения значения выпадающего списка
        /// </summary>
        public event Action<int> onDropdownValueUpdated;

        protected TMP_Dropdown dropdown;

        protected virtual void Awake() => dropdown = GetComponent<TMP_Dropdown>();
        protected virtual void OnEnable() => dropdown.onValueChanged.AddListener(OnDropdownAction);
        protected virtual void OnDisable() => dropdown.onValueChanged.RemoveListener(OnDropdownAction);

        private void OnDropdownAction(int value)
        {
            OnDropdownValueUpdated(value);
            onDropdownValueUpdated?.Invoke(value);
        }

        /// <summary>
        /// Код, выполняемый при изменении значения выпадающего списка
        /// </summary>
        protected virtual void OnDropdownValueUpdated(int value) {}
    }
}