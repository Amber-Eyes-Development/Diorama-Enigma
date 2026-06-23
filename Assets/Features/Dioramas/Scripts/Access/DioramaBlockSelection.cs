using Extensions.Data.InMemoryData.SelectionContext;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Выбранный игроком блок (Id) — мост «меню → игровая сцена»
    /// </summary>
    /// <remarks>
    /// Сессионный ассет: меню задаёт выбор, игровая сцена читает. Переживает переход сцен в памяти;
    /// диск не нужен — блок выбирается заново через меню. Блоки — SO-ассеты, контейнер не требуется
    /// </remarks>
    [CreateAssetMenu(menuName = "Dioramas/Block Selection", fileName = nameof(DioramaBlockSelection))]
    public sealed class DioramaBlockSelection : BaseSelectionContext
    {
        /// <summary> Id выбранного блока (пусто, если выбора нет) </summary>
        public string SelectedId => selectedId;

        [SerializeField] private string selectedId;

        /// <inheritdoc/>
        public override bool HasSelection => !string.IsNullOrEmpty(selectedId);

        /// <inheritdoc/>
        public override void Select(string selectionDataId)
        {
            selectedId = selectionDataId;
            OnSelectionChanged();
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            selectedId = string.Empty;
            OnSelectionChanged();
        }
    }
}
