using Extensions.Generics;
using Extensions.Log;

namespace Extensions.Input
{
    /// <summary>
    /// Кнопка сохранения привязок ввода
    /// </summary>
    public class SaveInputActionBindingsButton : AbstractButtonAction
    {
        public override void OnButtonClickAction()
        {
            if (BindingController.Instance == null)
            {
                ServiceDebug.LogError($"{nameof(BindingController)} не найден");
                return;
            }

            BindingController.Instance.SaveBindings();
        }

        public override int GetPriority => 0;
    }
}