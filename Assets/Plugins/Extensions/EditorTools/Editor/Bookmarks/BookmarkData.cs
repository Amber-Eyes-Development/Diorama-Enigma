using System;
using Extensions.Data.InMemoryData;

namespace Extensions.EditorTools.Bookmarks
{
    /// <summary>
    /// Типы закладок ObjectBookmarksWindow
    /// </summary>
    [Serializable]
    public enum BookmarkType
    {
        SceneObject,
        ProjectAsset
    }

    /// <summary>
    /// Структура данных сохранения закладок ObjectBookmarksWindow
    /// </summary>
    [Serializable]
    public sealed class BookmarkData : InMemoryDataEntry
    {
        public string ObjectId;
        
        public string Name;
        public BookmarkType Type;

        public string AssetPath;
        public string ScenePath;
    }
}