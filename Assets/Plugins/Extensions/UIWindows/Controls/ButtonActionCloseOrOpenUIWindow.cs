using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Кнопка для закрытия окна, если есть предыдущее окно, иначе открытия указанного окна
    /// </summary>
    public class ButtonActionCloseOrOpenUIWindow : ButtonActionOpenUIWindow
    {
        [Header("Параметры закрытия"), Space]
        [SerializeField] protected bool needToOpenPrevious = true;

        public override void OnButtonClickAction()
        {
            if (windowsController.HasPreviousWindow(parentUIWindow))
            {
                windowsController.CloseWindow(parentUIWindow, needToOpenPrevious);
                return;
            }

            base.OnButtonClickAction();
        }
    }
}