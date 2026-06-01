using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Extensions.EditorTools.Bookmarks;
using Extensions.Log;
using Object = UnityEngine.Object;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Окно для закладок объектов (Scene + Project)
    /// </summary>
    public sealed class BookmarksWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Bookmarks";
        private const string PROJECT_ASSETS_SECTION = "ProjectAssets";
        private const string UNKNOWN_SCENE_SECTION = "UnknownScene";
        
        [SerializeField]
        private BookmarksDataBase dataBase;

        private GUIStyle bookmarkStyle;
        private GUIStyle selectedBookmarkStyle;
        private Vector2 scroll = Vector2.zero;
        
        private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        [MenuItem("Tools/" + WINDOW_NAME)]
        public static void Open()
        {
            BookmarksWindow window = GetWindow<BookmarksWindow>();
            window.titleContent = new GUIContent(WINDOW_NAME);
            window.Show();
        }

        private void OnGUI()
        {
            if (dataBase == null)
            {
                EditorGUILayout.HelpBox("BookmarksDataBase не назначена", MessageType.Error);
                return;
            }

            if (bookmarkStyle == null) InitStyles();

            DrawAddButton();
            GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            scroll = GUILayout.BeginScrollView(scroll);
            DrawBookmarksList();
            GUILayout.EndScrollView();
        }

        #region UI

        private void InitStyles()
        {
            bookmarkStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                fixedHeight = EditorToolsConstraints.BASE_ELEMENT_HEIGHT,
                padding = new RectOffset(EditorToolsConstraints.TEXT_PADDING, EditorToolsConstraints.TEXT_PADDING, 0, 0)
            };

            selectedBookmarkStyle = new GUIStyle(bookmarkStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
        }

        private void DrawAddButton()
        {
            Object selected = Selection.activeObject;
            GUI.enabled = selected != null;

            if (GUILayout.Button("+ Добавить выбранный объект +", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                AddBookmark(selected);
            }

            GUI.enabled = true;
        }

        #endregion

        #region Bookmark List

        private void DrawBookmarksList()
        {
            IReadOnlyList<BookmarkData> items = dataBase.Data;

            if (items.Count == 0)
            {
                GUILayout.Label("Закладок пока нет");
                return;
            }

            Object selected = Selection.activeObject;
            string activeScene = SceneManager.GetActiveScene().path;

            Dictionary<string, List<BookmarkData>> groups = new Dictionary<string, List<BookmarkData>>();

            foreach (var b in items)
            {
                string key;

                if (b.Type == BookmarkType.ProjectAsset)
                    key = PROJECT_ASSETS_SECTION;
                else
                    key = string.IsNullOrEmpty(b.ScenePath) ? UNKNOWN_SCENE_SECTION : b.ScenePath;

                if (!groups.ContainsKey(key))
                    groups[key] = new List<BookmarkData>();

                groups[key].Add(b);
            }

            List<string> sortedKeys = new List<string>(groups.Keys);

            sortedKeys.Sort((a, b) =>
            {
                if (a == PROJECT_ASSETS_SECTION) return 1;
                if (b == PROJECT_ASSETS_SECTION) return -1;

                if (a == UNKNOWN_SCENE_SECTION) return 1;
                if (b == UNKNOWN_SCENE_SECTION) return -1;

                return String.Compare(a, b, StringComparison.Ordinal);
            });

            foreach (string key in sortedKeys)
            {
                string foldoutLabel;

                if (key == PROJECT_ASSETS_SECTION)
                    foldoutLabel = $"Ассеты проекта ({groups[key].Count})";
                else
                {
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(key);
                    bool isCurrent = key == activeScene;

                    foldoutLabel = isCurrent
                        ? $"Сцена: {sceneName} (текущая, {groups[key].Count})"
                        : $"Сцена: {sceneName} ({groups[key].Count})";
                }

                foldouts.TryAdd(key, true);

                foldouts[key] = EditorGUILayout.Foldout(foldouts[key], foldoutLabel, true);

                if (foldouts[key])
                {
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < groups[key].Count; i++)
                    {
                        BookmarkData data = groups[key][i];
                        DrawBookmarkRow(data, selected);
                    }

                    EditorGUI.indentLevel--;
                    GUILayout.Space(6);
                }
            }
        }

        private void DrawBookmarkRow(BookmarkData data, Object selected)
        {
            Object resolved = ResolveUnityObject(data);

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = GetRowColor(data, resolved);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT));

            DrawIcon(resolved);

            bool isSelected = resolved != null && resolved == selected;

            DrawNameButton(data, resolved, isSelected);
            DrawOpenFolderButtonIfNeeded(resolved);
            DrawSceneActionIfNeeded(data);
            DrawRemoveButton(data);

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevColor;
        }

        private void DrawIcon(Object resolved)
        {
            Texture icon = resolved != null
                ? EditorGUIUtility.ObjectContent(resolved, resolved.GetType()).image
                : EditorGUIUtility.IconContent("console.erroricon").image;

            GUILayout.Label(icon, GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT));
        }

        private void DrawNameButton(BookmarkData data, Object resolved, bool isSelected)
        {
            GUI.enabled = resolved != null;

            if (GUILayout.Button(data.Name, isSelected ? selectedBookmarkStyle : bookmarkStyle))
            {
                Selection.activeObject = resolved;
                EditorGUIUtility.PingObject(resolved);
            }

            GUI.enabled = true;
        }

        private void DrawOpenFolderButtonIfNeeded(Object resolved)
        {
            if (resolved == null) return;

            string assetPath = AssetDatabase.GetAssetPath(resolved);

            if (string.IsNullOrEmpty(assetPath) ||
                !AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.52f, 0.05f);

            GUIContent content = EditorGUIUtility.IconContent("FolderOpened Icon");

            if (GUILayout.Button(content, GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                OpenProjectFolder(assetPath);
            }

            GUI.backgroundColor = prevColor;
        }

        private void DrawSceneActionIfNeeded(BookmarkData data)
        {
            if (data.Type != BookmarkType.SceneObject || 
                !SceneFileExists(data))
            {
                return;
            }

            if (!IsSceneLoaded(data.ScenePath))
            {
                GUI.backgroundColor = EditorToolsConstraints.COLOR_YELLOW;

                if (GUILayout.Button("Открыть сцену", GUILayout.Width(110), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(data.ScenePath);
                    }
                }

                GUI.backgroundColor = Color.white;
            }
        }

        private void DrawRemoveButton(BookmarkData data)
        {
            GUI.backgroundColor = EditorToolsConstraints.COLOR_RED;

            if (GUILayout.Button("✕", GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                dataBase.Remove(data);
            }

            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Bookmark Logic

        private void AddBookmark(Object obj)
        {
            if (obj == null) return;

            BookmarkData data = new BookmarkData
            {
                Name = obj.name
            };

            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (!string.IsNullOrEmpty(assetPath))
            {
                data.Type = BookmarkType.ProjectAsset;
                data.AssetPath = assetPath;
                data.ObjectId = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();

                dataBase.Add(data);
                return;
            }

            GameObject gameObject = GetSceneGameObject(obj);

            if (gameObject == null)
            {
                ServiceDebug.LogWarning("Невозможно сохранить закладку на объект сцены");
                return;
            }

            data.Type = BookmarkType.SceneObject;
            data.ScenePath = gameObject.scene.path;
            data.ObjectId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();

            dataBase.Add(data);
        }

        private GameObject GetSceneGameObject(Object obj)
        {
            if (obj is GameObject go) return go;
            if (obj is Component comp) return comp.gameObject;

            return null;
        }

        private Object ResolveUnityObject(BookmarkData data)
        {
            Object resolvedById = ResolveByGlobalObjectId(data.ObjectId);

            if (resolvedById != null) return resolvedById;
            if (data.Type == BookmarkType.ProjectAsset) return AssetDatabase.LoadAssetAtPath<Object>(data.AssetPath);
            if (!IsSceneLoaded(data.ScenePath)) return null;

            return null;
        }

        private Object ResolveByGlobalObjectId(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return null;
            if (!GlobalObjectId.TryParse(objectId, out GlobalObjectId gid)) return null;

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
        }

        private void OpenProjectFolder(string folderPath)
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (folder == null) return;

            EditorUtility.FocusProjectWindow();

            EditorApplication.delayCall += () =>
            {
                Selection.activeObject = folder;

                EditorApplication.delayCall += () =>
                {
                    Type projectBrowserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
                    if (projectBrowserType == null)
                    {
                        AssetDatabase.OpenAsset(folder);
                        return;
                    }

                    EditorWindow projectBrowser = GetWindow(projectBrowserType);
                    if (projectBrowser == null)
                    {
                        AssetDatabase.OpenAsset(folder);
                        return;
                    }

                    MethodInfo showFolderContentsMethod = GetShowFolderContentsMethod(projectBrowserType);
                    if (showFolderContentsMethod == null)
                    {
                        AssetDatabase.OpenAsset(folder);
                        return;
                    }

                    showFolderContentsMethod.Invoke(projectBrowser, new object[] { folder.GetInstanceID(), true });
                };
            };
        }

        private MethodInfo GetShowFolderContentsMethod(Type projectBrowserType)
        {
            MethodInfo[] methods = projectBrowserType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                if (method.Name != "ShowFolderContents") continue;

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(int) &&
                    parameters[1].ParameterType == typeof(bool))
                {
                    return method;
                }
            }

            return null;
        }

        #endregion

        #region Helpers

        private bool IsSceneLoaded(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath)
                    return true;
            }
            return false;
        }

        private bool SceneFileExists(BookmarkData data)
        {
            return !string.IsNullOrEmpty(data.ScenePath) &&
                   System.IO.File.Exists(data.ScenePath);
        }

        private Color GetRowColor(BookmarkData data, Object resolved)
        {
            if (resolved != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(resolved);

                if (!string.IsNullOrEmpty(assetPath)) 
                    return EditorToolsConstraints.COLOR_CYAN;
            }

            if (data.Type == BookmarkType.ProjectAsset)
            {
                return resolved == null
                    ? EditorToolsConstraints.COLOR_RED
                    : EditorToolsConstraints.COLOR_CYAN;
            }

            if (!SceneFileExists(data))
                return EditorToolsConstraints.COLOR_RED;

            if (!IsSceneLoaded(data.ScenePath))
                return EditorToolsConstraints.COLOR_YELLOW;

            if (resolved == null)
                return EditorToolsConstraints.COLOR_LIGHT_RED;

            return EditorToolsConstraints.COLOR_LIGHT_GREEN;
        }

        #endregion
    }
}