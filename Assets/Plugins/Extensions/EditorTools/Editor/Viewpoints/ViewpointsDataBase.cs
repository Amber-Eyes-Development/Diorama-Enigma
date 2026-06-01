using Extensions.Data.InMemoryData;
using UnityEngine;

namespace Extensions.EditorTools.Viewpoints
{
    /// <summary>
    /// Сохранение и загрузка вьюпортов ViewpointsWindow
    /// </summary>
    [CreateAssetMenu(fileName = nameof(ViewpointsDataBase), menuName = "Extensions/EditorTools/" + nameof(ViewpointsDataBase))]
    public sealed class ViewpointsDataBase : InMemoryDataContainer<ViewpointsData> 
    { 
        protected override string SaveProfile => EditorToolsConstraints.PERSISTENT_SERVICE_PROFILE_NAME;
    }
}