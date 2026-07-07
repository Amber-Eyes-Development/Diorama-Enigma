using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Extensions.EditorTools.FolderIcons
{
    /// <summary>
    /// Перерисовывает окно Project: подменяет иконки папок по правилам FolderIconsSettings
    /// и (опционально) выделяет жирным имена папок внутри Features
    /// </summary>
    [InitializeOnLoad]
    public static class FolderIconsDrawer
    {
        // Выше этой высоты строки Project считаем плиточным (grid) отображением, ниже — списком
        private const float GRID_ROW_HEIGHT_THRESHOLD = 20f;

        // Высота подписи под иконкой в плиточном режиме
        private const float GRID_LABEL_HEIGHT = 14f;

        // Зазор между иконкой и подписью в режиме списка
        private const float LIST_LABEL_GAP = 2f;

        // Имя папки, прямые дети которой выделяются жирным
        private const string FEATURES_FOLDER = "Features";

        private static readonly Dictionary<string, Texture> iconByFolderName =
            new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);

        private static readonly Color NORMAL_BG_DARK = new Color(0.219f, 0.219f, 0.219f);
        private static readonly Color NORMAL_BG_LIGHT = new Color(0.7843f, 0.7843f, 0.7843f);
        private static readonly Color SELECTED_BG = new Color(0.172f, 0.364f, 0.529f);

        private static bool boldFeaturesChildren;
        private static bool cacheBuilt;

        private static GUIStyle listLabelStyle;
        private static GUIStyle gridLabelStyle;

        static FolderIconsDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        /// <summary>
        /// Пересобрать кэш правил и перерисовать окно Project (вызывается окном после изменений)
        /// </summary>
        public static void RefreshAndRepaint()
        {
            BuildCache();
            EditorApplication.RepaintProjectWindow();
        }

        private static void BuildCache()
        {
            cacheBuilt = true;
            iconByFolderName.Clear();
            boldFeaturesChildren = false;

            FolderIconsSettings settings = FolderIconsSettings.Find();
            if (settings == null) return;

            boldFeaturesChildren = settings.BoldFeaturesChildren;

            foreach (FolderIconRule rule in settings.Rules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.FolderName) || string.IsNullOrWhiteSpace(rule.IconName))
                    continue;

                Texture icon = EditorGUIUtility.IconContent(rule.IconName).image;
                if (icon == null) continue;

                // Последнее правило с тем же именем побеждает — предсказуемо для пользователя
                iconByFolderName[rule.FolderName.Trim()] = icon;
            }
        }

        private static void OnProjectWindowItemGUI(string guid, Rect rect)
        {
            if (!cacheBuilt) BuildCache();
            if (iconByFolderName.Count == 0 && !boldFeaturesChildren) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return;

            string folderName = Path.GetFileName(path);
            bool isGrid = rect.height > GRID_ROW_HEIGHT_THRESHOLD;

            if (boldFeaturesChildren && IsDirectFeaturesChild(path))
                DrawBoldLabel(rect, guid, folderName, isGrid);

            if (iconByFolderName.TryGetValue(folderName, out Texture icon) && icon != null)
                DrawIcon(rect, guid, icon, isGrid);
        }

        private static bool IsDirectFeaturesChild(string path)
        {
            // Прямой ребёнок: имя непосредственной родительской папки — ровно Features
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0) return false;

            string parent = path.Substring(0, lastSlash);
            int parentSlash = parent.LastIndexOf('/');
            string parentName = parentSlash >= 0 ? parent.Substring(parentSlash + 1) : parent;

            return parentName == FEATURES_FOLDER;
        }

        /// <summary>
        /// Закрашивает место стандартной иконки фоном строки (чтобы её не было видно)
        /// и поверх рисует кастомную иконку
        /// </summary>
        private static void DrawIcon(Rect rect, string guid, Texture icon, bool isGrid)
        {
            Rect iconRect = GetIconRect(rect, isGrid);

            EditorGUI.DrawRect(iconRect, GetRowBackgroundColor(guid));
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// Габариты стандартной иконки папки внутри строки — отдельно для списка и плиток
        /// </summary>
        private static Rect GetIconRect(Rect rect, bool isGrid)
        {
            if (!isGrid)
            {
                // Список / дерево: квадрат во всю высоту строки у левого края
                float size = rect.height;
                return new Rect(rect.x, rect.y, size, size);
            }

            // Плитки: квадрат по центру верхней части строки, над подписью
            float gridSize = Mathf.Min(rect.width, rect.height - GRID_LABEL_HEIGHT);
            float x = rect.x + (rect.width - gridSize) / 2f;

            return new Rect(x, rect.y, gridSize, gridSize);
        }

        /// <summary>
        /// Поверх стандартной подписи закрашивает фон строки и рисует имя жирным.
        /// Фон строки берётся приблизительно по теме/выделению (без учёта ховера)
        /// </summary>
        private static void DrawBoldLabel(Rect rect, string guid, string folderName, bool isGrid)
        {
            bool selected = Array.IndexOf(Selection.assetGUIDs, guid) >= 0;

            Rect labelRect;
            GUIStyle style;

            if (!isGrid)
            {
                float iconSize = rect.height;
                labelRect = new Rect(rect.x + iconSize + LIST_LABEL_GAP, rect.y, rect.width - iconSize - LIST_LABEL_GAP, rect.height);
                style = GetListLabelStyle();
            }
            else
            {
                labelRect = new Rect(rect.x, rect.yMax - GRID_LABEL_HEIGHT, rect.width, GRID_LABEL_HEIGHT);
                style = GetGridLabelStyle();
            }

            EditorGUI.DrawRect(labelRect, GetRowBackgroundColor(guid));

            style.normal.textColor = selected ? Color.white : EditorStyles.label.normal.textColor;
            GUI.Label(labelRect, folderName, style);
        }

        /// <summary>
        /// Приблизительный фон строки Project по теме и факту выделения (без учёта ховера)
        /// </summary>
        private static Color GetRowBackgroundColor(string guid)
        {
            if (Array.IndexOf(Selection.assetGUIDs, guid) >= 0)
                return SELECTED_BG;

            return EditorGUIUtility.isProSkin ? NORMAL_BG_DARK : NORMAL_BG_LIGHT;
        }

        private static GUIStyle GetListLabelStyle()
        {
            return listLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold
            };
        }

        private static GUIStyle GetGridLabelStyle()
        {
            return gridLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
        }
    }
}
