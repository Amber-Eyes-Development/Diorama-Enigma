using System;
using System.Collections;
using System.Collections.Generic;
using Extensions.Coroutines;
using Extensions.Log;
using Extensions.RuntimeReferences;
using Extensions.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Контроллер сцен
    /// </summary>
    /// <remarks>
    /// Управляет переключением сцен. Сцена загрузки поднимается оверлеем поверх остальных,
    /// прежние сцены выгружаются под её прикрытием, а целевая сцена освобождает экран загрузки
    /// только после собственной готовности (опциональный <see cref="SceneLoadCoordinator"/>),
    /// выдержки минимального времени показа и анимации исчезновения экрана
    /// </remarks>
    public sealed class SceneController : MonoBehaviourSingleton<SceneController>
    {
        #region Events

        /// <summary>
        /// Событие начала загрузки сцены
        /// </summary>
        public event Action onLoadingStart;
        /// <summary>
        /// Событие обновления прогресса загрузки
        /// </summary>
        /// <returns>
        /// Процент загрузки от 0 до 1
        /// </returns>
        public event Action<float> onLoadingProgressUpdate;
        /// <summary>
        /// Событие окончания загрузки сцены
        /// </summary>
        public event Action onSceneLoaded;

        #endregion

        #region Свойства

        /// <summary>
        /// Состояние перехода между сценами
        /// </summary>
        /// <returns>true если в стадии перехода, false если переход завершен</returns>
        public bool IsTransitionInProgress => isTransitionInProgress;
        /// <summary>
        /// Текущий прогресс
        /// </summary>
        public float CurrentProgress => currentProgress;

        #endregion

        #region Параметры

        [Header("Стартовая сцена"), Space]
        [SerializeField] private SceneID firstScene;
        [SerializeField] private bool loadFirstSceneOnStart = true;

        [Header("Настройки перехода"), Space]
        [Range(0f, 4f), Tooltip("Задержка перед началом перехода (0 — на следующий кадр)")]
        [SerializeField] private float transitionDelay;
        [Min(0f), Tooltip("Таймаут попытки загрузки сцены перед уходом в fallback-сцену")]
        [SerializeField] private float targetTimeout = 20f;
        [Min(0f), Tooltip("Минимальное время показа сцены загрузки: экран не отпускается раньше, даже если целевая сцена готова")]
        [SerializeField] private float minLoadingDuration = 1f;

        [Header("Сцены проекта"), Space]
        [SerializeField] private SceneID loadingScene;
        [SerializeField] private List<SceneBinding> scenes = new();

        [Header("Каналы рантайм-ссылок"), Space]
        [Tooltip("Опционально. Канал экрана загрузки. Не задан — переход без анимаций появления/исчезновения")]
        [SerializeField] private LoadingScreenReference loadingScreenChannel;
        [Tooltip("Опционально. Канал координатора готовности сцены. Не задан — переход без ожидания готовности сцены")]
        [SerializeField] private SceneLoadCoordinatorReference coordinatorChannel;

        [Header("Таймауты ожиданий"), Space]
        [Range(0f, 20f), Tooltip("Ожидание публикации канала (экран/координатор) после загрузки сцены")]
        [SerializeField] private float referenceWaitTimeout = 5f;
        [Range(0f, 30f), Tooltip("Ожидание готовности целевой сцены от координатора")]
        [SerializeField] private float readinessTimeout = 20f;
        [Range(0f, 5f), Tooltip("Фолбэк-ожидание завершения анимации появления/исчезновения экрана")]
        [SerializeField] private float screenAnimationTimeout = 1f;

        #endregion

        #region Внутренние переменные

        private const float MaxFrameDelta = 0.1f;
        private const float ProgressEpsilon = 0.0001f;
        private const float LoadPhasePortion = 0.5f;

        private bool isTransitionInProgress;
        private float currentProgress;
        private bool lastLoadTimedOut;
        private string lastLoadedSceneName;

        private CoroutineTask transitionTask;

        #endregion

        #region MonoBehaviour

        protected override void Awake()
        {
            base.Awake();
            transitionTask = new CoroutineTask(this);
        }

        private void Start()
        {
            if (!loadFirstSceneOnStart)
                return;

            if (firstScene == null)
            {
                ServiceDebug.LogError($"Не назначена стартовая сцена в {nameof(SceneController)}");
                return;
            }

            LoadSceneByID(firstScene.Id);
        }

        #endregion

        #region Загрузка сцены

        /// <summary>
        /// Загрузка сцены по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор</param>
        /// <param name="additive">Аддитивная подгрузка поверх (без экрана загрузки и выгрузки текущих)</param>
        public void LoadSceneByID(string id, bool additive = false)
        {
            if (isTransitionInProgress)
                return;

            if (string.IsNullOrEmpty(id))
            {
                ServiceDebug.LogError($"Пустой идентификатор при вызове {nameof(LoadSceneByID)}");
                return;
            }

            if (!TryGetSceneName(id, out string sceneName))
            {
                ServiceDebug.LogError($"Сцена с идентификатором «{id}» не найдена");
                return;
            }

            isTransitionInProgress = true;
            transitionTask.Start(TransitionRoutine(sceneName, additive));
        }

        private IEnumerator TransitionRoutine(string targetSceneName, bool additive)
        {
            if (transitionDelay <= 0f)
                yield return null;
            else
                yield return new WaitForSecondsRealtime(transitionDelay);

            if (additive)
            {
                yield return AdditiveLoadRoutine(targetSceneName);
                isTransitionInProgress = false;
                yield break;
            }

            currentProgress = 0f;
            onLoadingStart?.Invoke();

            // Сцены, которые предстоит выгрузить под прикрытием экрана загрузки (снимок до его поднятия)
            List<Scene> scenesToUnload = SnapshotScenesToUnload();

            // 1. Поднимаем сцену загрузки оверлеем поверх текущих
            float loadingShownTime = Time.unscaledTime;
            bool overlayShown = false;
            if (TryGetLoadingSceneName(out string loadingSceneName))
            {
                AsyncOperation loadLoading = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);
                if (loadLoading != null)
                {
                    yield return WaitForAsyncOperation(loadLoading, targetTimeout, loadingSceneName, false);
                    overlayShown = !lastLoadTimedOut;
                    loadingShownTime = Time.unscaledTime;
                }
                else
                {
                    ServiceDebug.LogError($"Не удалось начать загрузку loading-сцены «{loadingSceneName}»");
                }
            }
            else
            {
                ServiceDebug.LogWarning($"Не найдена loading-сцена в {nameof(SceneController)} — переход без экрана загрузки");
            }

            // 2. Дожидаемся публикации экрана и проигрываем его появление
            ILoadingScreen loadingScreen = null;
            if (loadingScreenChannel != null)
            {
                yield return WaitForReferencePublished(loadingScreenChannel, referenceWaitTimeout, "экран загрузки");
                loadingScreen = loadingScreenChannel.HasValue ? loadingScreenChannel.Current : null;
            }

            if (loadingScreen != null)
                yield return WaitForCallback(loadingScreen.PlayIntro, screenAnimationTimeout, "появление экрана");

            // 3. Выгружаем прежние сцены — их уже закрывает оверлей
            yield return UnloadScenesRoutine(scenesToUnload);

            // 4. Грузим целевую сцену (с фолбэком в стартовую)
            yield return LoadTargetRoutine(targetSceneName);

            if (string.IsNullOrEmpty(lastLoadedSceneName))
            {
                // Целевая и фолбэк не загрузились — снимаем оверлей, чтобы переход не завис
                ServiceDebug.LogError($"Не удалось загрузить целевую сцену «{targetSceneName}»");
                yield return ReleaseLoadingOverlay(loadingScreen, overlayShown, loadingSceneName, loadingShownTime);
                isTransitionInProgress = false;
                yield break;
            }

            Scene loadedScene = SceneManager.GetSceneByName(lastLoadedSceneName);
            if (loadedScene.IsValid())
                SceneManager.SetActiveScene(loadedScene);

            // 5. Ждём готовность целевой сцены (опционально)
            yield return WaitForReadinessRoutine();

            currentProgress = 1f;
            onLoadingProgressUpdate?.Invoke(currentProgress);

            // 6. Выдерживаем минимальное время показа экрана загрузки
            yield return WaitForMinimumDuration(loadingShownTime);

            onSceneLoaded?.Invoke();

            // 7. Проигрываем исчезновение экрана и снимаем оверлей
            yield return ReleaseLoadingOverlay(loadingScreen, overlayShown, loadingSceneName, loadingShownTime);

            isTransitionInProgress = false;
        }

        /// <summary>
        /// Прямая аддитивная подгрузка сцены поверх текущих без экрана загрузки
        /// </summary>
        private IEnumerator AdditiveLoadRoutine(string targetSceneName)
        {
            currentProgress = 0f;
            onLoadingStart?.Invoke();

            AsyncOperation loadTarget = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (loadTarget != null)
            {
                yield return WaitForAsyncOperation(loadTarget, targetTimeout, targetSceneName, true);

                Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);
                if (!lastLoadTimedOut && loadedScene.IsValid())
                    SceneManager.SetActiveScene(loadedScene);
            }
            else
            {
                ServiceDebug.LogError($"Не удалось начать загрузку сцены «{targetSceneName}»");
            }

            currentProgress = 1f;
            onSceneLoaded?.Invoke();
        }

        /// <summary>
        /// Загрузка целевой сцены аддитивно с уходом в фолбэк-сцену при таймауте.
        /// Итоговое имя загруженной сцены — в <see cref="lastLoadedSceneName"/> (пусто при провале)
        /// </summary>
        private IEnumerator LoadTargetRoutine(string targetSceneName)
        {
            lastLoadedSceneName = null;

            string sceneToLoad = targetSceneName;
            bool usingFallback = false;
            float loadScale = coordinatorChannel != null ? LoadPhasePortion : 1f;

            while (true)
            {
                AsyncOperation loadTarget = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

                if (loadTarget != null)
                {
                    yield return WaitForAsyncOperation(loadTarget, targetTimeout, sceneToLoad, true, 0f, loadScale);
                    if (!lastLoadTimedOut)
                    {
                        lastLoadedSceneName = sceneToLoad;
                        yield break;
                    }
                }
                else
                {
                    ServiceDebug.LogError($"Не удалось начать загрузку сцены «{sceneToLoad}»");
                }

                if (usingFallback)
                {
                    ServiceDebug.LogError($"Фолбэк-сцена «{sceneToLoad}» также не загрузилась. Переход прерван");
                    yield break;
                }

                if (!TryGetFallbackSceneName(out string fallbackSceneName))
                    yield break;

                if (fallbackSceneName == sceneToLoad)
                {
                    ServiceDebug.LogError($"Фолбэк совпадает с целевой сценой «{sceneToLoad}». Переход прерван");
                    yield break;
                }

                ServiceDebug.LogWarning($"Переход в фолбэк-сцену «{fallbackSceneName}»");
                sceneToLoad = fallbackSceneName;
                usingFallback = true;
            }
        }

        /// <summary>
        /// Ожидание готовности целевой сцены через координатор (если канал задан и опубликован)
        /// </summary>
        private IEnumerator WaitForReadinessRoutine()
        {
            if (coordinatorChannel == null)
                yield break;

            yield return WaitForReferencePublished(coordinatorChannel, referenceWaitTimeout, "координатор готовности сцены");

            if (!coordinatorChannel.HasValue)
            {
                ServiceDebug.LogWarning("Координатор готовности не опубликован — готовность сцены не отслеживается");
                yield break;
            }

            SceneLoadCoordinator coordinator = coordinatorChannel.Current;
            float timer = 0f;

            while (coordinator != null && !coordinator.IsComplete)
            {
                currentProgress = LoadPhasePortion + coordinator.AggregateProgress * (1f - LoadPhasePortion);
                onLoadingProgressUpdate?.Invoke(currentProgress);

                timer += Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
                if (timer >= readinessTimeout)
                {
                    ServiceDebug.LogError($"Таймаут готовности целевой сцены ({readinessTimeout}с). " +
                                          $"Экран загрузки будет отпущен");
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Проигрывание исчезновения экрана и выгрузка сцены загрузки
        /// </summary>
        private IEnumerator ReleaseLoadingOverlay(
            ILoadingScreen loadingScreen,
            bool overlayShown,
            string loadingSceneName,
            float loadingShownTime)
        {
            // Гарантируем минимальное время показа и на путях аварийного снятия оверлея
            yield return WaitForMinimumDuration(loadingShownTime);

            if (loadingScreen != null)
                yield return WaitForCallback(loadingScreen.PlayOutro, screenAnimationTimeout, "исчезновение экрана");

            if (overlayShown)
                yield return UnloadSceneByNameRoutine(loadingSceneName);
        }

        #endregion

        #region Ожидания

        /// <summary>
        /// Ожидание завершения асинхронной операции с таймаутом по «зависанию» прогресса
        /// </summary>
        private IEnumerator WaitForAsyncOperation(
            AsyncOperation operation,
            float timeoutSeconds,
            string label,
            bool trackProgress,
            float progressOffset = 0f,
            float progressScale = 1f)
        {
            lastLoadTimedOut = false;

            float stallTimer = 0f;
            float lastProgress = operation.progress;

            while (!operation.isDone)
            {
                if (operation.progress > lastProgress + ProgressEpsilon)
                {
                    lastProgress = operation.progress;
                    stallTimer = 0f;
                }
                else
                {
                    stallTimer += Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
                }

                if (trackProgress)
                {
                    currentProgress = progressOffset + operation.progress * progressScale;
                    onLoadingProgressUpdate?.Invoke(currentProgress);
                }

                if (stallTimer >= timeoutSeconds)
                {
                    ServiceDebug.LogError($"Таймаут загрузки: «{label}». progress={operation.progress}, " +
                                          $"allowSceneActivation={operation.allowSceneActivation}");

                    lastLoadTimedOut = true;
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Ожидание публикации объекта в канал рантайм-ссылки с таймаутом
        /// </summary>
        private IEnumerator WaitForReferencePublished<T>(RuntimeReference<T> channel, float timeout, string label)
            where T : class
        {
            if (channel == null || channel.HasValue)
                yield break;

            float timer = 0f;
            while (!channel.HasValue)
            {
                timer += Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
                if (timer >= timeout)
                {
                    ServiceDebug.LogWarning($"Таймаут ожидания публикации: «{label}» ({timeout}с)");
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Запуск действия с колбэком завершения и ожидание этого колбэка с таймаутом
        /// </summary>
        private IEnumerator WaitForCallback(Action<Action> begin, float timeout, string label)
        {
            bool done = false;
            begin(() => done = true);

            float timer = 0f;
            while (!done)
            {
                timer += Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
                if (timer >= timeout)
                {
                    ServiceDebug.LogWarning($"Таймаут ожидания анимации: «{label}» ({timeout}с)");
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Выдержка минимального времени показа экрана загрузки от момента его появления
        /// </summary>
        private IEnumerator WaitForMinimumDuration(float shownTime)
        {
            if (minLoadingDuration <= 0f)
                yield break;

            float remaining = minLoadingDuration - (Time.unscaledTime - shownTime);
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);
        }

        #endregion

        #region Выгрузка сцен

        private List<Scene> SnapshotScenesToUnload()
        {
            List<Scene> result = new();
            Scene ownScene = gameObject.scene;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                // Не выгружаем сцену самого контроллера, чтобы не уничтожить его посреди перехода
                if (scene == ownScene)
                    continue;

                result.Add(scene);
            }

            return result;
        }

        private IEnumerator UnloadScenesRoutine(List<Scene> scenesToUnload)
        {
            foreach (Scene scene in scenesToUnload)
            {
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null)
                    yield return unload;
            }
        }

        private IEnumerator UnloadSceneByNameRoutine(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                yield break;

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload != null)
                yield return unload;
        }

        #endregion

        #region Внутренние геттеры

        private bool TryGetFallbackSceneName(out string sceneName)
        {
            sceneName = string.Empty;

            if (firstScene == null)
            {
                ServiceDebug.LogError($"Фолбэк невозможен: не назначена стартовая сцена");
                return false;
            }

            if (!TryGetSceneName(firstScene.Id, out sceneName))
            {
                ServiceDebug.LogError($"Фолбэк невозможен: стартовая сцена не найдена в списке сцен");
                return false;
            }

            return true;
        }

        private bool TryGetSceneName(string id, out string sceneName)
        {
            sceneName = string.Empty;

            foreach (SceneBinding binding in scenes)
            {
                if (binding == null)
                    continue;

                if (binding.Id == null)
                    continue;

                if (binding.Id.Id == id)
                {
                    sceneName = binding.SceneName;
                    return !string.IsNullOrEmpty(sceneName);
                }
            }

            return false;
        }

        private bool TryGetLoadingSceneName(out string sceneName)
        {
            sceneName = string.Empty;

            if (loadingScene == null)
                return false;

            return TryGetSceneName(loadingScene.Id, out sceneName);
        }

        #endregion

        #region Дополнительные структуры

        /// <summary>
        /// Пара (связка) сцена-идентификатор
        /// </summary>
        [Serializable]
        private class SceneBinding
        {
            [field: SerializeField] public SceneID Id { get; private set; }

            [field: SerializeField] public string SceneName { get; private set; }
        }

        #endregion
    }
}
