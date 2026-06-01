using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция кнопки
    /// </summary>
    [RequireComponent(typeof(Button))]
    public abstract class AbstractButton : MonoBehaviour
    {
        protected Button button;

        protected virtual void Awake() => button = GetComponent<Button>();

        protected virtual void OnEnable() => button.onClick.AddListener(OnButtonClicked);

        protected virtual void OnDisable() => button.onClick.RemoveListener(OnButtonClicked);

        protected virtual void OnButtonClicked() { }
    }
}