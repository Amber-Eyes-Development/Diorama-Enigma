using System;
using System.Collections;
using System.Collections.Generic;
using Extensions.Coroutines;
using Extensions.Log;
using Extensions.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Контроллер сцен
    /// </summary>
    /// <remarks>
    /// Управляет переключением сцен и ответственен за переходы между ними
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
        [Range(0f, 4f), Tooltip("Задержка перед загрузкой сцены (0 — моментальная загрузка на следующий кадр)")]
        [SerializeField] private float transitionDelay;
        [Min(0f), Tooltip("Таймаут попытки загрузки сцены перед уходом в fallback-сцену")]
        [SerializeField] private float targetTimeout = 20f;

        [Header("Сцены проекта"), Space]
        [SerializeField] private SceneID loadingScene;
        [SerializeField] private List<SceneBinding> scenes = new();

        #endregion
        
        #region Внутренние переменные

        private const float MaxFrameDelta = 0.1f;
        private const float ProgressEpsilon = 0.0001f;

        private bool isTransitionInProgress;
        private float currentProgress;
        private bool lastLoadTimedOut;

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
        /// <param name="additive">Аддитивно или с закрытием активных</param>
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
        
        /// <summary>
        /// Ожидание завершения асинхронной операции с таймаутом по «зависанию» прогресса
        /// </summary>
        private IEnumerator WaitForAsyncOperation(
            AsyncOperation operation,
            float timeoutSeconds,
            string label,
            bool trackProgress)
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
                    currentProgress = operation.progress;
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

        private IEnumerator TransitionRoutine(string targetSceneName, bool additive)
        {
            if (transitionDelay <= 0f)
                yield return null;
            else
                yield return new WaitForSecondsRealtime(transitionDelay);

            currentProgress = 0f;
            onLoadingStart?.Invoke();

            if (TryGetLoadingSceneName(out string loadingSceneName))
            {
                AsyncOperation loadLoading = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Single);
                if (loadLoading != null)
                    yield return WaitForAsyncOperation(loadLoading, targetTimeout, loadingSceneName, false);
                else
                    ServiceDebug.LogError($"Не удалось начать загрузку loading-сцены «{loadingSceneName}»");
            }
            else
            {
                ServiceDebug.LogError($"Не найдена loading-сцена в {nameof(SceneController)}");
            }

            string sceneToLoad = targetSceneName;
            bool loadAdditive = additive;
            bool usingFallback = false;

            while (true)
            {
                LoadSceneMode mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
                AsyncOperation loadTarget = SceneManager.LoadSceneAsync(sceneToLoad, mode);

                if (loadTarget != null)
                {
                    yield return WaitForAsyncOperation(loadTarget, targetTimeout, sceneToLoad, true);
                    if (!lastLoadTimedOut)
                        break;
                }
                else
                {
                    ServiceDebug.LogError($"Не удалось начать загрузку сцены «{sceneToLoad}»");
                }

                if (usingFallback)
                {
                    ServiceDebug.LogError($"Фолбэк-сцена «{sceneToLoad}» также не загрузилась. Переход прерван");
                    isTransitionInProgress = false;
                    yield break;
                }

                if (!TryGetFallbackSceneName(out string fallbackSceneName))
                {
                    isTransitionInProgress = false;
                    yield break;
                }

                if (fallbackSceneName == sceneToLoad)
                {
                    ServiceDebug.LogError($"Фолбэк совпадает с целевой сценой «{sceneToLoad}». Переход прерван");
                    isTransitionInProgress = false;
                    yield break;
                }

                ServiceDebug.LogWarning($"Переход в фолбэк-сцену «{fallbackSceneName}»");
                sceneToLoad = fallbackSceneName;
                loadAdditive = false;
                usingFallback = true;
            }

            if (loadAdditive)
            {
                Scene loadedScene = SceneManager.GetSceneByName(sceneToLoad);
                if (loadedScene.IsValid())
                    SceneManager.SetActiveScene(loadedScene);
            }

            currentProgress = 1f;
            isTransitionInProgress = false;
            onSceneLoaded?.Invoke();
        }

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

        #endregion
        
        #region Внутренние геттеры
        
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