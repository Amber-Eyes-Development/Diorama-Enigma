using System;
using TMPro;
using UnityEngine;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция поля ввода TMP
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public abstract class AbstractInputField : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое после изменения текста поля ввода
        /// </summary>
        public event Action<string> onInputFieldValueUpdated;

        protected TMP_InputField inputField;

        protected virtual void Awake() => inputField = GetComponent<TMP_InputField>();
        protected virtual void OnEnable() => inputField.onValueChanged.AddListener(OnInputFieldAction);
        protected virtual void OnDisable() => inputField.onValueChanged.RemoveListener(OnInputFieldAction);

        private void OnInputFieldAction(string value)
        {
            OnInputFieldValueUpdated(value);
            onInputFieldValueUpdated?.Invoke(value);
        }

        /// <summary>
        /// Код, выполняемый при изменении текста поля ввода
        /// </summary>
        protected virtual void OnInputFieldValueUpdated(string value) {}
    }
}