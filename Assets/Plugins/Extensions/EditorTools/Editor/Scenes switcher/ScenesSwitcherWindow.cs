using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Cysharp.Threading.Tasks;
using Extensions.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Навигатор сцен проекта
    /// </summary>
    public sealed class ScenesSwitcherWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Scenes";

        private static GUIStyle _sceneButtonStyle;
        private static GUIStyle _activeSceneStyle;

        private Vector2 _scroll;

        [MenuItem("Tools/" + WINDOW_NAME)]
        public static void ShowWindow() =>
            GetWindow<ScenesSwitcherWindow>(WINDOW_NAME);

        #region GUI

        private void OnGUI()
        {
            if (_sceneButtonStyle == null || _activeSceneStyle == null)
                InitStyles();

            DrawPlayControls();
            GUILayout.Space(6);

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawScenesList();
            GUILayout.EndScrollView();
        }

        private void InitStyles()
        {
            _sceneButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = EditorToolsConstraints.BASE_ELEMENT_HEIGHT
            };

            _activeSceneStyle = new GUIStyle(_sceneButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green }
            };
        }

        private void DrawPlayControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorApplication.isPlaying)
                {
                    DrawPlayModeControls();
                }
                else
                {
                    DrawEditModeControls();
                }
            }
        }

        private void DrawEditModeControls()
        {
            SetBG(EditorToolsConstraints.COLOR_GREEN);
            if (GUILayout.Button("▶ Active", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                PlayCurrentScene();

            SetBG(EditorToolsConstraints.COLOR_GREEN);
            if (GUILayout.Button("▶ Project", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                PlayProject();

            SetBG(EditorToolsConstraints.COLOR_PURPLE);
            if (GUILayout.Button("▶ Clean Project", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();

                JsonSaveLoad.DeleteAllAsync().ContinueWith(PlayProject).Forget();
            }

            ResetBG();
        }

        private void DrawPlayModeControls()
        {
            if (EditorApplication.isPaused)
            {
                SetBG(EditorToolsConstraints.COLOR_GREEN);
                if (GUILayout.Button("▶ Resume", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                    EditorApplication.isPaused = false;
            }
            else
            {
                SetBG(EditorToolsConstraints.COLOR_YELLOW);
                if (GUILayout.Button("⏸ Pause", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                    EditorApplication.isPaused = true;
            }

            SetBG(EditorToolsConstraints.COLOR_RED);
            if (GUILayout.Button("■ Stop", GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                EditorApplication.isPlaying = false;

            ResetBG();
        }

        private void DrawScenesList()
        {
            var buildScenes = EditorBuildSettings.scenes;
            if (buildScenes == null || buildScenes.Length == 0)
            {
                GUILayout.Label("⚠ No scenes in Build Settings!");
                return;
            }

            string activePath = SceneManager.GetActiveScene().path;

            foreach (var buildScene in buildScenes)
            {
                DrawSceneRow(buildScene, activePath);
            }
        }

        private void DrawSceneRow(EditorBuildSettingsScene buildScene, string activePath)
        {
            string path = buildScene.path;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            string name = Path.GetFileNameWithoutExtension(path);
            bool isActive = activePath == path;
            bool isOpened = IsSceneOpened(path);

            SetBG(isActive ? EditorToolsConstraints.COLOR_LIGHT_GREEN : Color.white);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSceneMainButton(path, name, isActive);
                DrawSceneAdditiveButtons(path, isOpened, isActive, activePath);
                DrawScenePingButton(path);
            }

            ResetBG();
        }

        private void DrawSceneMainButton(string path, string name, bool isActive)
        {
            bool canSwitch = !EditorApplication.isPlaying;

            using (new EditorGUI.DisabledScope(!canSwitch))
            {
                if (GUILayout.Button(name, isActive ? _activeSceneStyle : _sceneButtonStyle))
                    OpenScene(path);
            }
        }

        private void DrawSceneAdditiveButtons(string path, bool isOpened, bool isActive, string activePath)
        {
            bool playing = EditorApplication.isPlaying;

            bool canOpenAdditive = !isOpened && !playing;
            bool canCloseAdditive = isOpened &&
                                    SceneManager.sceneCount > 1 &&
                                    path != activePath &&
                                    !playing;

            if (canOpenAdditive)
            {
                SetBG(EditorToolsConstraints.COLOR_LIGHT_GREEN);
                if (GUILayout.Button("+", GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                    ToggleAdditiveScene(path, false);
            }

            if (canCloseAdditive)
            {
                SetBG(EditorToolsConstraints.COLOR_LIGHT_RED);
                if (GUILayout.Button("−", GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                    ToggleAdditiveScene(path, true);
            }

            ResetBG();
        }

        private void DrawScenePingButton(string path)
        {
            SetBG(EditorToolsConstraints.COLOR_CYAN);
            if (GUILayout.Button("●", GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT), GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                PingScene(path);
            ResetBG();
        }

        #endregion

        #region SceneOperation

        private static void OpenScene(string path)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot switch scenes while in Play Mode!");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            EditorApplication.isPlaying = false;
        }

        private static void PlayProject()
        {
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled)
                    continue;

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
                return;
            }
        }

        private static void PlayCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();

            if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.path))
            {
                Debug.LogError("Current scene is not valid or not saved! Save it before playing.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorApplication.isPlaying = true;
        }

        private static void ToggleAdditiveScene(string path, bool isOpened)
        {
            var scene = SceneManager.GetSceneByPath(path);

            if (isOpened)
            {
                // Нельзя закрыть последнюю сцену или активную сцену
                if (SceneManager.sceneCount <= 1 || scene == SceneManager.GetActiveScene())
                {
                    Debug.LogWarning("Cannot close active or last open scene!");
                    return;
                }

                if (scene.isDirty)
                {
                    if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene }))
                        return;
                }

                EditorSceneManager.CloseScene(scene, true);
            }
            else
            {
                if (IsOpenedSceneDirty())
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        return;
                }

                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
        }

        #endregion

        #region Helpers

        private static bool IsSceneOpened(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == path && s.isLoaded)
                    return true;
            }

            return false;
        }

        private static bool IsOpenedSceneDirty()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            }

            return false;
        }

        private static void PingScene(string path)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset != null)
                EditorGUIUtility.PingObject(sceneAsset);
        }

        private static void SetBG(Color color) => GUI.backgroundColor = color;
        private static void ResetBG() => GUI.backgroundColor = Color.white;

        #endregion
    }
}
