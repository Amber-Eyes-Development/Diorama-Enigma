using System;

namespace Extensions.EditorTools.FolderIcons
{
    /// <summary>
    /// Правило подмены иконки: все папки проекта с именем FolderName получают
    /// встроенную иконку редактора с именем IconName (как в EditorGUIUtility.IconContent)
    /// </summary>
    [Serializable]
    public sealed class FolderIconRule
    {
        public string FolderName;
        public string IconName;
    }
}
