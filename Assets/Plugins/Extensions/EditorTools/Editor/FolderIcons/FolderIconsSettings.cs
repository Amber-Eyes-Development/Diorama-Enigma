using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools.FolderIcons
{
    /// <summary>
    /// Набор правил «имя папки → иконка» для кастомной отрисовки в окне Project.
    /// Единый ассет на проект, создаётся автоматически при первом обращении из окна
    /// </summary>
    public sealed class FolderIconsSettings : ScriptableObject
    {
        private const string ASSET_PATH =
            "Assets/Plugins/Extensions/EditorTools/Editor/FolderIcons/FolderIconsSettings.asset";

        /// <summary>Правила подмены иконок в порядке добавления</summary>
        public IReadOnlyList<FolderIconRule> Rules => rules;

        /// <summary>Выделять жирным имена всех папок, лежащих внутри папки Features</summary>
        public bool BoldFeaturesChildren => boldFeaturesChildren;

        [SerializeField]
        private List<FolderIconRule> rules = new List<FolderIconRule>();

        [SerializeField]
        private bool boldFeaturesChildren;

        /// <summary>
        /// Найти существующий ассет настроек без создания (для рисовальщика на старте домена)
        /// </summary>
        public static FolderIconsSettings Find()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(FolderIconsSettings));
            if (guids.Length == 0) return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<FolderIconsSettings>(path);
        }

        /// <summary>
        /// Получить ассет настроек, создав его при отсутствии (для окна редактирования)
        /// </summary>
        public static FolderIconsSettings GetOrCreate()
        {
            FolderIconsSettings existing = Find();
            if (existing != null) return existing;

            FolderIconsSettings settings = CreateInstance<FolderIconsSettings>();

            AssetDatabase.CreateAsset(settings, ASSET_PATH);
            AssetDatabase.SaveAssets();

            return settings;
        }
    }
}
