using Extensions.Generics;
using Extensions.Log;

namespace Extensions.Input
{
    /// <summary>
    /// Кнопка сброса всех привязок ввода
    /// </summary>
    public class ResetAllInputActionBindingsButton : AbstractButtonAction
    {
        public override void OnButtonClickAction()
        {
            if (BindingController.Instance == null)
            {
                ServiceDebug.LogError($"{nameof(BindingController)} не найден");
                return;
            }

            BindingController.Instance.ResetAllBindings();
        }
    }
}