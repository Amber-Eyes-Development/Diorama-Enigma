using Extensions.Log;
using Extensions.SceneFlow;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка перехода к следующему блоку: выбирает следующий по реестру блок и перезагружает игровую сцену
    /// </summary>
    /// <remarks> Текущий блок — выбранный в контексте (загруженный сессией); целевая сцена — поле базовой кнопки </remarks>
    public sealed class DioramaNextBlockButtonAction : LoadSceneButtonAction
    {
        [Header("Переход к следующему блоку"), Space]
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Выбор блока (ассет-мост)")]
        [SerializeField] private DioramaBlockSelection blockSelection;
        [Tooltip("Выбор диорамы (ассет-мост)")]
        [SerializeField] private DioramaSelection dioramaSelection;

        public override void OnButtonClickAction()
        {
            if (!SelectNextBlock()) return;
            base.OnButtonClickAction();
        }

        private bool SelectNextBlock()
        {
            if (access == null || blockSelection == null)
            {
                ServiceDebug.LogError("access или blockSelection не назначены");
                return false;
            }

            var current = access.BlockById(blockSelection.SelectedId);
            if (current == null)
            {
                ServiceDebug.LogWarning("Текущий блок не определён");
                return false;
            }

            var next = access.NextBlock(current);
            if (next == null)
            {
                ServiceDebug.LogWarning("Следующего блока нет — текущий последний в реестре");
                return false;
            }

            blockSelection.Select(next.Id);
            if (dioramaSelection != null) dioramaSelection.Clear(); // спавнер сам выберет стартовую диораму нового блока
            return true;
        }
    }
}
