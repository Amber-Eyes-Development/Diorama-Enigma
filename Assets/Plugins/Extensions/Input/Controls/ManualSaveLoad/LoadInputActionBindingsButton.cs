using Extensions.Generics;
using Extensions.Log;

namespace Extensions.Input
{
    /// <summary>
    /// Кнопка загрузки привязок ввода
    /// </summary>
    public class LoadInputActionBindingsButton : AbstractButtonAction
    {
        public override void OnButtonClickAction()
        {
            if (BindingController.Instance == null)
            {
                ServiceDebug.LogError($"{nameof(BindingController)} не найден");
                return;
            }

            BindingController.Instance.LoadBindings();
        }

        public override int GetPriority => 0;
    }
}