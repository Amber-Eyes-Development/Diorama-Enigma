using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions.EditorTools.Viewpoints
{
    /// <summary>
    /// Навигатор точек обзора на сценах
    /// </summary>
    public sealed class EditorViewpointsWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Viewpoints";

        [SerializeField]
        private ViewpointsDataBase dataBaseBase;

        private GUIStyle _labelHeader;
        private GUIStyle _rowButton;

        private string _newViewName = "Viewpoint";
        private Vector2 _scroll;

        [MenuItem("Tools/" + WINDOW_NAME)]
        public static void Open()
        {
            EditorViewpointsWindow window = GetWindow<EditorViewpointsWindow>();
            window.titleContent = new GUIContent(WINDOW_NAME);
            window.Show();
        }

        private void InitStyles()
        {
            _labelHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                padding = new RectOffset(0, 0, 4, 4)
            };

            _rowButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = EditorToolsConstraints.BASE_ELEMENT_HEIGHT,
                padding = new RectOffset(EditorToolsConstraints.TEXT_PADDING, EditorToolsConstraints.TEXT_PADDING, 0, 0)
            };
        }

        private void OnGUI()
        {
            if (dataBaseBase == null)
            {
                EditorGUILayout.HelpBox("ViewpointsDataBase не назначена", MessageType.Error);
                return;
            }

            if (_rowButton == null || _labelHeader == null)
                InitStyles();

            GUILayout.Space(4);
            GUILayout.Label("Точки камеры", _labelHeader);

            using (new EditorGUILayout.HorizontalScope())
            {
                _newViewName = GUILayout.TextField(_newViewName);

                if (GUILayout.Button("Сохранить точку", GUILayout.Width(140)))
                    AddViewpoint(_newViewName);
            }

            GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawViewpointsUI();
            GUILayout.EndScrollView();
        }

        private void DrawViewpointsUI()
        {
            string currentScene = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(currentScene))
            {
                GUILayout.Label("Сцена не сохранена");
                return;
            }

            IReadOnlyList<ViewpointsData> items = dataBaseBase.Data;
            bool any = false;
            ViewpointsData toRemove = null;

            foreach (var vp in items)
            {
                if (vp.ScenePath != currentScene)
                    continue;

                any = true;

                if (DrawViewpointRow(vp))
                    toRemove = vp;
            }

            if (toRemove != null)
            {
                dataBaseBase.Remove(toRemove);
            }

            if (!any)
                GUILayout.Label("Нет точек для текущей сцены");
        }

        private bool DrawViewpointRow(ViewpointsData vp)
        {
            bool remove = false;

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                if (GUILayout.Button(vp.Name, _rowButton))
                    GoToViewpoint(vp);

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("✕", GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                    remove = true;
                GUI.backgroundColor = Color.white;
            }

            return remove;
        }

        private void AddViewpoint(string name)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null)
                return;

            ViewpointsData data = new ViewpointsData
            {
                Name = string.IsNullOrEmpty(name) ? "View" : name,
                ScenePath = scene.path,
                Position = sv.camera.transform.position,
                Rotation = sv.camera.transform.eulerAngles,
                Size = sv.size,
                Orthographic = sv.orthographic,
                Mode2D = sv.in2DMode
            };

            dataBaseBase.Add(data);
        }

        private void GoToViewpoint(ViewpointsData vp)
        {
            EditorApplication.delayCall += () =>
            {
                SceneView sv = SceneView.lastActiveSceneView;
                if (sv == null)
                    return;

                sv.Focus();
                sv.LookAt(vp.Position, Quaternion.Euler(vp.Rotation), vp.Size);
                sv.orthographic = vp.Orthographic;
                sv.in2DMode = vp.Mode2D;
                sv.Repaint();
            };
        }
    }
}
