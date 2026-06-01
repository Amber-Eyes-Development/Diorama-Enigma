using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Кнопка для закрытия окна интерфейса
    /// </summary>
    public class ButtonActionCloseUIWindow : UIWindowControlButtonAction
    {
        [Header("Параметры закрытия"), Space]
        [SerializeField] protected bool needToOpenPrevious = true;

        public override void OnButtonClickAction() => windowsController.CloseWindow(
            parentUIWindow, needToOpenPrevious);

        public override int GetPriority => 0;
    }
}