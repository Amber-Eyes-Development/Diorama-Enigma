using System.Collections.Generic;
using System.IO;
using Extensions.Log;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Extensions.EditorTools.Viewpoints
{
    /// <summary>
    /// Навигатор точек обзора: частные (привязаны к сцене) и общие (видны на любой сцене)
    /// </summary>
    public sealed class EditorViewpointsWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Viewpoints";
        private const string GLOBAL_SECTION = "Global";

        [SerializeField]
        private ViewpointsDataBase dataBase;

        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        private string _newViewName = "Viewpoint";
        private ViewpointScope _newScope = ViewpointScope.Scene;
        private Vector2 _scroll;

        [MenuItem("Tools/" + WINDOW_NAME)]
        public static void Open()
        {
            EditorViewpointsWindow window = GetWindow<EditorViewpointsWindow>();
            window.titleContent = new GUIContent(WINDOW_NAME);
            window.Show();
        }

        private void OnEnable()
        {
            if (dataBase == null)
                dataBase = FindDataBase();
        }

        #region UI

        private void OnGUI()
        {
            if (dataBase == null)
            {
                EditorGUILayout.HelpBox("ViewpointsDataBase is not assigned", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorToolsStyles.WindowPadding))
            {
                DrawAddPanel();

                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorToolsGUI.Separator();
                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

                _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                DrawViewpointsList();
                GUILayout.EndScrollView();
            }
        }

        private void DrawAddPanel()
        {
            _newViewName = EditorGUILayout.TextField(_newViewName);

            GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawScopeToggle(ViewpointScope.Scene, EditorToolsConstraints.ICON_SCENE, "Scene");
                DrawScopeToggle(ViewpointScope.Global, EditorToolsConstraints.ICON_GLOBAL, "Global");

                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_GREEN))
                {
                    if (EditorToolsGUI.IconButton(EditorToolsConstraints.ICON_SAVE, "Save", GUILayout.Width(110)))
                        AddViewpoint(_newViewName, _newScope);
                }
            }
        }

        // Кнопка выбора области видимости: растягивается по свободному месту, активная подсвечена
        private void DrawScopeToggle(ViewpointScope scope, string icon, string label)
        {
            bool active = _newScope == scope;

            using (new GUIBackgroundColorScope(active ? EditorToolsConstraints.COLOR_LIGHT_GREEN : Color.white))
            {
                if (EditorToolsGUI.IconButton(icon, label, GUILayout.ExpandWidth(true)))
                    _newScope = scope;
            }
        }

        private void DrawViewpointsList()
        {
            IReadOnlyList<ViewpointsData> items = dataBase.Data;

            string currentScene = EditorSceneManager.GetActiveScene().path;
            bool sceneSaved = !string.IsNullOrEmpty(currentScene);

            List<ViewpointsData> globals = new List<ViewpointsData>();
            List<ViewpointsData> sceneLocals = new List<ViewpointsData>();

            foreach (ViewpointsData vp in items)
            {
                if (vp.Scope == ViewpointScope.Global)
                    globals.Add(vp);
                else if (sceneSaved && vp.ScenePath == currentScene)
                    sceneLocals.Add(vp);
            }

            ViewpointsData toRemove = DrawSection(GLOBAL_SECTION, $"Global ({globals.Count})", globals);

            if (sceneSaved)
            {
                string sceneName = Path.GetFileNameWithoutExtension(currentScene);
                ViewpointsData removed = DrawSection(currentScene, $"Scene: {sceneName} (current, {sceneLocals.Count})", sceneLocals);
                if (removed != null)
                    toRemove = removed;
            }
            else
            {
                EditorGUILayout.HelpBox("Scene is not saved — local points are unavailable", MessageType.Info);
            }

            if (globals.Count == 0 && sceneLocals.Count == 0)
                GUILayout.Label("No points yet", EditorToolsStyles.MutedLabel);

            if (toRemove != null)
                dataBase.Remove(toRemove);
        }

        private ViewpointsData DrawSection(string key, string label, List<ViewpointsData> list)
        {
            _foldouts.TryAdd(key, true);
            _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], label, true, EditorToolsStyles.BoldFoldout);

            if (!_foldouts[key])
                return null;

            ViewpointsData toRemove = null;

            EditorGUI.indentLevel++;

            if (list.Count == 0)
                GUILayout.Label("— empty —", EditorToolsStyles.MutedLabel);
            else
                foreach (ViewpointsData vp in list)
                    if (DrawViewpointRow(vp))
                        toRemove = vp;

            EditorGUI.indentLevel--;
            GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            return toRemove;
        }

        private bool DrawViewpointRow(ViewpointsData vp)
        {
            bool remove = false;

            using (new GUIBackgroundColorScope(GetScopeColor(vp.Scope)))
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                GUIContent content = EditorToolsGUI.IconText(EditorToolsConstraints.ICON_CAMERA, vp.Name);
                if (GUILayout.Button(content, EditorToolsStyles.RowButton))
                    GoToViewpoint(vp);

                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_RED))
                {
                    if (EditorToolsGUI.IconButtonSquare(EditorToolsConstraints.ICON_REMOVE, EditorToolsConstraints.SYMBOL_REMOVE, "Remove"))
                        remove = true;
                }
            }

            return remove;
        }

        private static Color GetScopeColor(ViewpointScope scope)
        {
            switch (scope)
            {
                case ViewpointScope.Scene:  return Color.white;
                case ViewpointScope.Global: return EditorToolsConstraints.COLOR_CYAN;
                default:
                    ServiceDebug.LogError($"Неизвестная область видимости точки обзора: {scope}");
                    return Color.white;
            }
        }

        #endregion

        #region Logic

        private void AddViewpoint(string name, ViewpointScope scope)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null)
            {
                ServiceDebug.LogWarning("Активного окна Scene нет — точку сохранить нельзя");
                return;
            }

            string scenePath = string.Empty;

            if (scope == ViewpointScope.Scene)
            {
                scenePath = EditorSceneManager.GetActiveScene().path;
                if (string.IsNullOrEmpty(scenePath))
                {
                    ServiceDebug.LogWarning("Сцена не сохранена — частную точку сохранить нельзя");
                    return;
                }
            }

            ViewpointsData data = new ViewpointsData
            {
                Name = string.IsNullOrEmpty(name) ? "Viewpoint" : name,
                Scope = scope,
                ScenePath = scenePath,
                Pivot = sv.pivot,
                Rotation = sv.rotation,
                Size = sv.size,
                Orthographic = sv.orthographic,
                Mode2D = sv.in2DMode
            };

            dataBase.Add(data);
        }

        private void GoToViewpoint(ViewpointsData vp)
        {
            EditorApplication.delayCall += () =>
            {
                SceneView sv = SceneView.lastActiveSceneView;
                if (sv == null)
                    return;

                sv.Focus();

                // Проекцию задаём до LookAt: дистанция камеры от pivot считается с её учётом
                sv.in2DMode = vp.Mode2D;
                sv.orthographic = vp.Orthographic;

                sv.LookAt(vp.Pivot, vp.Rotation, vp.Size);
                sv.Repaint();
            };
        }

        private static ViewpointsDataBase FindDataBase()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ViewpointsDataBase)}");
            if (guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ViewpointsDataBase>(path);
        }

        #endregion
    }
}
