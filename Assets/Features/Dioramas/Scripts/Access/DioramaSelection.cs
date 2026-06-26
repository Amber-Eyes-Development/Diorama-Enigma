using Extensions.Data.InMemoryData.SelectionContext;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Выбранная игроком диорама (Id) — мост меню-сцена
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Diorama Selection", fileName = nameof(DioramaSelection))]
    public sealed class DioramaSelection : BaseSelectionContext, ISingleSelectionContext
    {
        /// <summary> Id выбранной диорамы (пусто, если выбора нет) </summary>
        public string SelectedId => DioramaProgressStore.LoadLastActive();

        public override bool HasSelection => !string.IsNullOrEmpty(SelectedId);

        public override void Select(string selectionDataId)
        {
            DioramaProgressStore.SaveLastActive(selectionDataId);
            OnSelectionChanged();
        }

        public override void Clear()
        {
            DioramaProgressStore.SaveLastActive(null);
            OnSelectionChanged();
        }
    }
}
