using System;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция переключателя
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public abstract class AbstractToggle : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое после изменения состояния переключателя
        /// </summary>
        public event Action<bool> onToggled;
        
        /// <summary>
        /// Состояние тоггла
        /// </summary>
        public bool IsOn => toggle.isOn;
        
        protected Toggle toggle;

        protected virtual void Awake() => toggle = GetComponent<Toggle>();

        protected virtual void OnEnable() => toggle.onValueChanged.AddListener(OnToggleAction);

        protected virtual void OnDisable() => toggle.onValueChanged.RemoveListener(OnToggleAction);

        protected virtual void OnToggleAction(bool state)
        {
            OnToggled(state);
            onToggled?.Invoke(state);
        }
        
        /// <summary>
        /// Код, выполняемый при изменении состояния переключателя
        /// </summary>
        public abstract void OnToggled(bool state);
    }
}
