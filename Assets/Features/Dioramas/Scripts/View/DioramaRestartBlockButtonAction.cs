using Extensions.Generics;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка-зажатие рестарта выбранного блока «как впервые»: сброс шагов и внутриблоковой разблокировки;
    /// разблокировка самого блока и весь последующий прогресс сохраняются
    /// </summary>
    /// <remarks> Зажатие само служит подтверждением — отдельного окна подтверждения не нужно </remarks>
    public sealed class DioramaRestartBlockButtonAction : AbstractHoldButtonAction
    {
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Выбранный блок (ассет-мост)")]
        [SerializeField] private DioramaBlockSelection blockSelection;

        public override void OnButtonClickAction()
        {
            if (access == null || blockSelection == null)
            {
                ServiceDebug.LogError("access или blockSelection не назначены");
                return;
            }

            var block = access.BlockById(blockSelection.SelectedId);
            if (block == null)
            {
                ServiceDebug.LogWarning("Нет выбранного блока для рестарта");
                return;
            }

            access.RestartBlock(block);
        }
    }
}
