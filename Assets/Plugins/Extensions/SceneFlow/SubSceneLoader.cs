using System;
using System.Collections;
using Extensions.Coroutines;
using Extensions.Log;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Загрузчик аддитивных под-сцен поверх постоянных сцен
    /// </summary>
    /// <remarks>
    /// Опциональное расширение над <see cref="SceneController"/>: грузит/выгружает/меняет под-сцену поверх
    /// корневой и boot-сцен, не трогая их. Экран загрузки переиспользуется у контроллера сцен
    /// (<see cref="SceneController.ShowLoadingOverlay"/>/<see cref="SceneController.ReleaseLoadingOverlay"/>).
    /// Начальную под-сцену грузят с <c>showLoadingScreen=false</c> (оверлей уже поднят переходом корневой сцены),
    /// а её прогресс отдают координатору готовности через шаг загрузки; рантайм-смену — с <c>showLoadingScreen=true</c>
    /// </remarks>
    public sealed class SubSceneLoader : MonoBehaviour
    {
        /// <summary>Под-сцена загружена и активирована</summary>
        /// <returns>Имя загруженной под-сцены</returns>
        public event Action<string> onSubSceneChanged;

        /// <summary>Имя текущей загруженной под-сцены (или null)</summary>
        public string CurrentSubScene { get; private set; }
        /// <summary>Идёт загрузка/смена под-сцены</summary>
        public bool IsBusy { get; private set; }
        /// <summary>Прогресс текущей загрузки (0..1)</summary>
        public float Progress { get; private set; }

        [Tooltip("Активировать загруженную под-сцену как активную сцену")]
        [SerializeField] private bool setLoadedAsActive = true;
        [Min(0f), Tooltip("Таймаут «зависания» прогресса загрузки под-сцены, сек")]
        [SerializeField] private float loadTimeout = 20f;
        [Min(0), Tooltip("Кадры «оседания» под экраном после загрузки — дать под-сцене инициализироваться до показа")]
        [SerializeField] private int settleFrames = 1;

        private const float MaxFrameDelta = 0.1f;
        private const float ProgressEpsilon = 0.0001f;

        private CoroutineTask task;

        private void Awake() => task = new CoroutineTask(this);

        /// <summary>
        /// Загрузить/сменить под-сцену поверх постоянных сцен
        /// </summary>
        /// <remarks>Опционально выгружает предыдущую под-сцену и поднимает экран загрузки на время смены</remarks>
        /// <param name="sceneName">Имя загружаемой под-сцены</param>
        /// <param name="unloadSceneName">Имя выгружаемой под-сцены (или null)</param>
        /// <param name="showLoadingScreen">Поднять экран загрузки на время смены</param>
        public void LoadSubScene(string sceneName, string unloadSceneName = null, bool showLoadingScreen = true)
        {
            if (IsBusy)
            {
                ServiceDebug.LogWarning($"{nameof(SubSceneLoader)}: смена под-сцены уже идёт — запрос «{sceneName}» проигнорирован");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                ServiceDebug.LogError($"{nameof(SubSceneLoader)}: пустое имя под-сцены");
                return;
            }

            IsBusy = true;
            Progress = 0f;
            task.Start(LoadRoutine(sceneName, unloadSceneName, showLoadingScreen));
        }

        private IEnumerator LoadRoutine(string sceneName, string unloadSceneName, bool showLoadingScreen)
        {
            SceneController controller = SceneController.Instance;
            SceneController.LoadingOverlayHandle overlay = null;

            if (showLoadingScreen && controller != null)
            {
                overlay = new SceneController.LoadingOverlayHandle();
                yield return controller.ShowLoadingOverlay(overlay);
            }

            // Сначала грузим новую под-сцену; прежнюю выгружаем только при успехе — иначе игрок остаётся в старой
            bool loaded = false;
            yield return LoadAdditiveRoutine(sceneName, success => loaded = success);

            if (loaded)
            {
                if (!string.IsNullOrEmpty(unloadSceneName) && unloadSceneName != sceneName)
                    yield return UnloadRoutine(unloadSceneName);

                if (setLoadedAsActive)
                {
                    Scene scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.IsValid() && scene.isLoaded)
                        SceneManager.SetActiveScene(scene);
                }

                // Держим экран, пока под-сцена инициализируется (Awake/OnEnable/Start), чтобы не показать её сырой
                for (int i = 0; i < settleFrames; i++)
                    yield return null;

                CurrentSubScene = sceneName;
                Progress = 1f;

                onSubSceneChanged?.Invoke(sceneName);
            }
            else
            {
                ServiceDebug.LogError($"{nameof(SubSceneLoader)}: под-сцена «{sceneName}» не загружена — прежняя оставлена. Проверьте Build Settings");
            }

            if (overlay != null && controller != null)
                yield return controller.ReleaseLoadingOverlay(overlay);

            IsBusy = false;
        }

        private IEnumerator LoadAdditiveRoutine(string sceneName, Action<bool> onDone)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                ServiceDebug.LogError($"{nameof(SubSceneLoader)}: не удалось начать загрузку под-сцены «{sceneName}»");
                onDone(false);
                yield break;
            }

            float stallTimer = 0f;
            float lastProgress = op.progress;

            while (!op.isDone)
            {
                if (op.progress > lastProgress + ProgressEpsilon)
                {
                    lastProgress = op.progress;
                    stallTimer = 0f;
                }
                else
                {
                    stallTimer += Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
                }

                Progress = op.progress;

                if (stallTimer >= loadTimeout)
                {
                    ServiceDebug.LogError($"{nameof(SubSceneLoader)}: таймаут загрузки под-сцены «{sceneName}»");
                    onDone(false);
                    yield break;
                }

                yield return null;
            }

            onDone(true);
        }

        private IEnumerator UnloadRoutine(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                yield break;

            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            if (op != null)
                yield return op;
        }
    }
}
