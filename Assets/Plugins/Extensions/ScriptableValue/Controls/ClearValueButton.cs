using Extensions.Generics;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Кнопка очистки или сброса <see cref="ScriptableValue"/>
    /// </summary>
    public class ClearValueButton : AbstractButtonAction
    {
        [SerializeField] protected BaseScriptableValue scriptableValue;
        [Tooltip("Сбросить до дефолтного, иначе очистить")]
        [SerializeField] protected bool resetToDefault = true;

        /// <summary>
        /// Нажатие на кнопку
        /// </summary>
        public override void OnButtonClickAction()
        {
            if (scriptableValue == null) return;

            if (resetToDefault)
                scriptableValue.ResetToDefault();
            else
                scriptableValue.Clear();
        }
    }
}