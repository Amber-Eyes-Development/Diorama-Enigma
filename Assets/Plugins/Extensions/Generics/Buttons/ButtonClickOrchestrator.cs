using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Оркестратор очередности действий кнопок <see cref="AbstractButtonAction"/>
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class ButtonClickOrchestrator : BaseButtonOrchestrator
    {
        private void Awake() => button = GetComponent<Button>();
        private void OnEnable() => button.onClick.AddListener(ProcessActions);
        private void OnDisable() => button.onClick.RemoveListener(ProcessActions);
    }
}