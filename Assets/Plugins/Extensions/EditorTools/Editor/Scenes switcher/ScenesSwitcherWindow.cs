using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Cysharp.Threading.Tasks;
using Extensions.Data;
using Extensions.Log;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Навигатор сцен проекта
    /// </summary>
    [InitializeOnLoad]
    public sealed class ScenesSwitcherWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Scenes";
        private const string LAST_OPENED_KEY = "ScenesSwitcher.LastOpened";

        private static GUIStyle _sceneButtonStyle;
        private static GUIStyle _activeSceneStyle;

        private Vector2 _scroll;

        [MenuItem("Tools/" + WINDOW_NAME)]
        public static void ShowWindow() =>
            GetWindow<ScenesSwitcherWindow>(WINDOW_NAME);

        static ScenesSwitcherWindow()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += RestoreLastOpenedScene;
        }

        #region GUI

        private void OnGUI()
        {
            if (_sceneButtonStyle == null || _activeSceneStyle == null)
                InitStyles();

            using (new EditorGUILayout.VerticalScope(EditorToolsStyles.WindowPadding))
            {
                DrawPlayControls();

                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorToolsGUI.Separator();
                GUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);

                _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                DrawScenesList();
                GUILayout.EndScrollView();
            }
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
            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_GREEN))
            {
                if (EditorToolsGUI.IconButton("SceneAsset Icon", "Active"))
                    PlayCurrentScene();

                if (EditorToolsGUI.IconButton("PlayButton", "Project"))
                    PlayProject();
            }

            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_PURPLE))
            {
                if (EditorToolsGUI.IconButton("PlayButton", "Clean Project"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();

                    CleanProjectAndPlay().Forget();
                }

                var clearSavesContent = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Trash")) { tooltip = "Delete saves" };
                if (GUILayout.Button(clearSavesContent,
                        GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT),
                        GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();

                    JsonSaveLoad.DeleteAllAsync().Forget();
                }
            }
        }

        private void DrawPlayModeControls()
        {
            if (EditorApplication.isPaused)
            {
                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_GREEN))
                {
                    if (EditorToolsGUI.IconButton("PlayButton", "Resume"))
                        EditorApplication.isPaused = false;
                }
            }
            else
            {
                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_YELLOW))
                {
                    if (EditorToolsGUI.IconButton("PauseButton", "Pause"))
                        EditorApplication.isPaused = true;
                }
            }

            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_RED))
            {
                if (EditorToolsGUI.IconButton("PreMatQuad", "Stop"))
                    EditorApplication.isPlaying = false;
            }
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

            using (new GUIBackgroundColorScope(isActive ? EditorToolsConstraints.COLOR_LIGHT_GREEN : Color.white))
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSceneMainButton(path, name, isActive);
                DrawSceneAdditiveButtons(path, isOpened, isActive, activePath);
                DrawScenePingButton(path);
            }
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
                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_LIGHT_GREEN))
                {
                    if (GUILayout.Button("+",
                            GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT),
                            GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                        ToggleAdditiveScene(path, false);
                }
            }

            if (canCloseAdditive)
            {
                using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_LIGHT_RED))
                {
                    if (GUILayout.Button("−",
                            GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT),
                            GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                        ToggleAdditiveScene(path, true);
                }
            }
        }

        private void DrawScenePingButton(string path)
        {
            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_CYAN))
            {
                if (GUILayout.Button("●",
                        GUILayout.Width(EditorToolsConstraints.BASE_ELEMENT_HEIGHT),
                        GUILayout.Height(EditorToolsConstraints.BASE_ELEMENT_HEIGHT)))
                    PingScene(path);
            }
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
            LastOpenedScenePath = path;
            EditorApplication.isPlaying = false;
        }

        private static async UniTaskVoid CleanProjectAndPlay()
        {
            await JsonSaveLoad.DeleteAllAsync();

            EditorApplication.delayCall += PlayProject;
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
                ServiceDebug.LogError("Current scene is not valid or not saved! Save it before playing.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            LastOpenedScenePath = activeScene.path;
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
                    ServiceDebug.LogWarning("Cannot close active or last open scene!");
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

        #region Persistence

        private static string LastOpenedScenePath
        {
            get => JsonSaveLoad.Load(LAST_OPENED_KEY, string.Empty, EditorToolsConstraints.PERSISTENT_SERVICE_PROFILE_NAME);
            set => JsonSaveLoad.SaveAsync(value, LAST_OPENED_KEY, EditorToolsConstraints.PERSISTENT_SERVICE_PROFILE_NAME).Forget();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += RestoreLastOpenedScene;
        }

        private static void RestoreLastOpenedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string path = LastOpenedScenePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var active = SceneManager.GetActiveScene();
            if (active.path == path)
                return;

            if (active.isDirty)
                return;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
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

        #endregion
    }
}
