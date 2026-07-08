using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Кнопка с зажатием для закрытия окна интерфейса
    /// </summary>
    public class HoldButtonActionCloseUIWindow : UIWindowControlHoldButtonAction
    {
        [Header("Параметры закрытия"), Space]
        [SerializeField] protected bool needToOpenPrevious = true;

        public override void OnButtonClickAction() => windowsController.CloseWindow(
            parentUIWindow, needToOpenPrevious);

        public override int GetPriority => 0;
    }
}