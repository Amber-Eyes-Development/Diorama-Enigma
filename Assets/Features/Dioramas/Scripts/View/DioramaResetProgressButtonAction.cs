using Extensions.Generics;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка-зажатие сброса всего прогресса (новая игра): все шаги, разблокировки диорам/блоков и выбор сбрасываются
    /// </summary>
    /// <remarks> Зажатие само служит подтверждением — отдельного окна подтверждения не нужно </remarks>
    public sealed class DioramaResetProgressButtonAction : AbstractHoldButtonAction
    {
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;

        public override void OnButtonClickAction()
        {
            if (access == null)
            {
                ServiceDebug.LogError($"{nameof(access)} не назначен");
                return;
            }

            access.ResetAllProgress();
        }
    }
}
