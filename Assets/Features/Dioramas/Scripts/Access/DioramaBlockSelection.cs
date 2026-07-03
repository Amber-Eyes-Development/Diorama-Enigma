using Extensions.Data.InMemoryData.SelectionContext;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Выбранный игроком блок (локация, Id) — мост меню-сцена
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Block Selection", fileName = nameof(DioramaBlockSelection))]
    public sealed class DioramaBlockSelection : BaseSelectionContext, ISingleSelectionContext
    {
        /// <summary> Id выбранного блока (пусто, если выбора нет) </summary>
        public string SelectedId => DioramaProgressStore.LoadSelectedBlock();

        public override bool HasSelection => !string.IsNullOrEmpty(SelectedId);

        public override void Select(string selectionDataId)
        {
            DioramaProgressStore.SaveSelectedBlock(selectionDataId);
            OnSelectionChanged();
        }

        public override void Clear()
        {
            DioramaProgressStore.SaveSelectedBlock(null);
            OnSelectionChanged();
        }
    }
}
