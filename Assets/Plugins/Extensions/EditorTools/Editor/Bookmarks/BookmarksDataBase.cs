using Extensions.Data.InMemoryData;
using UnityEngine;

namespace Extensions.EditorTools.Bookmarks
{
    /// <summary>
    /// Сохранение и загрузка закладок ObjectBookmarksWindow
    /// </summary>
    [CreateAssetMenu(fileName = nameof(BookmarksDataBase), menuName = "Extensions/EditorTools/" + nameof(BookmarksDataBase))]
    public class BookmarksDataBase : InMemoryDataContainer<BookmarkData> 
    { 
        protected override string SaveProfile => EditorToolsConstraints.PERSISTENT_SERVICE_PROFILE_NAME;
    }
}