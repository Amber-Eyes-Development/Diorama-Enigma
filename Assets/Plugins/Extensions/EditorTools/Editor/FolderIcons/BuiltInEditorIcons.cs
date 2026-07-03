using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools.FolderIcons
{
    /// <summary>
    /// Перечисляет имена встроенных иконок редактора из внутреннего editor-ассетбандла.
    /// Имена пригодны для EditorGUIUtility.IconContent (вариант под тему подбирается автоматически)
    /// </summary>
    internal static class BuiltInEditorIcons
    {
        private static List<string> names;

        /// <summary>Отсортированный список уникальных имён встроенных иконок (лениво, с кэшем)</summary>
        public static IReadOnlyList<string> Names => names ??= Load();

        private static List<string> Load()
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                AssetBundle bundle = GetEditorAssetBundle();

                if (bundle != null)
                {
                    foreach (string assetName in bundle.GetAllAssetNames())
                    {
                        // Внутри бандла иконки лежат под путём вида ".../icons/..."
                        if (assetName.IndexOf("icons/", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        if (!assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                            !assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string iconName = Path.GetFileNameWithoutExtension(assetName);

                        // Тёмные/контрастные варианты схлопываем к базовому имени
                        if (iconName.StartsWith("d_", StringComparison.Ordinal))
                            iconName = iconName.Substring(2);

                        if (!string.IsNullOrEmpty(iconName))
                            unique.Add(iconName);
                    }
                }
            }
            catch (Exception exception)
            {
                // Перечисление опирается на внутренний API — при отказе остаётся ручной ввод имени
                Debug.LogWarning($"[FolderIcons] Не удалось перечислить встроенные иконки: {exception.Message}");
            }

            List<string> result = new List<string>(unique);
            result.Sort(StringComparer.OrdinalIgnoreCase);

            return result;
        }

        private static AssetBundle GetEditorAssetBundle()
        {
            MethodInfo method = typeof(EditorGUIUtility).GetMethod(
                "GetEditorAssetBundle",
                BindingFlags.NonPublic | BindingFlags.Static);

            return method?.Invoke(null, null) as AssetBundle;
        }
    }
}
