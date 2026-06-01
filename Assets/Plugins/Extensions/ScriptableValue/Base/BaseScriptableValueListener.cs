using Extensions.Helpers;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Базовый слушатель изменений <see cref="ScriptableValue{TValue}"/>
    /// </summary>
    /// <typeparam name="TValue">Тип хранимого значения</typeparam>
    /// <typeparam name="TScriptableValue">Тип ScriptableValue-хранилища</typeparam>
    public abstract class BaseScriptableValueListener<TValue, TScriptableValue> : MonoBehaviour
        where TScriptableValue : ScriptableValue<TValue>
    {
        [SerializeField] protected TScriptableValue scriptableValue;

        protected virtual void OnEnable()
        {
            if (Logic.IsNull(scriptableValue, nameof(scriptableValue))) return;
            scriptableValue.onValueChanged += OnValueChanged;
        }

        protected virtual void OnDisable()
        {
            if (Logic.IsNull(scriptableValue, nameof(scriptableValue))) return;
            scriptableValue.onValueChanged -= OnValueChanged;
        }

        protected abstract void OnValueChanged(TValue value);
    }
}