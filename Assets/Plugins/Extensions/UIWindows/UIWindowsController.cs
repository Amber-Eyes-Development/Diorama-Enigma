using System;
using System.Collections.Generic;
using Extensions.Log;
using Extensions.Singleton;
using UnityEngine;

namespace Extensions.UIWindows
{
    /// <summary>
    /// Контроллер окон пользовательского интерфейса
    /// </summary>
    public class UIWindowsController : MonoBehaviourSingleton<UIWindowsController>
    {
        #region События
        
        /// <summary>
        /// Изменение последнего открытого окна
        /// </summary>
        public event Action<UIWindow> onLastOpenedWindowChanged;
        
        #endregion
        
        #region Свойства

        /// <summary>
        /// Последнее открытое окно
        /// </summary>
        public UIWindow LastOpenedWindow => lastOpenedWindow;

        #endregion

        #region Параметры

        [Header("Настройки"), Space]
        [SerializeField] protected UIWindow startWindow;
        [SerializeField] protected List<UIWindow> preparedUIWindows = new();
        [SerializeField] protected Transform root;

        #endregion

        #region Переменные

        protected readonly List<UIWindow> openedUIWindows = new();
        protected UIWindow lastOpenedWindow;
        
        protected readonly HashSet<UIWindow> activeUIWindows = new();
        protected readonly Stack<UIWindowID> navigationStack = new();

        #endregion

        #region MonoBehaviour

        protected override void Awake()
        {
            base.Awake();

            if (root == null) root = transform;

            if (startWindow == null)
            {
                ServiceDebug.LogError($"{nameof(startWindow)} не назначено в {nameof(UIWindowsController)}");
                return;
            }
            
            OpenStartWindow();
        }

        #endregion

        #region Открытие окон

        /// <summary>
        /// Открытие нового окна по нажатию на кнопку
        /// </summary>
        /// <summary>
        /// Открытие нового окна по нажатию на кнопку
        /// </summary>
        public void OpenWindow(
            string window,
            UIWindow parentWindow,
            bool needToCloseThis = true,
            bool needToCloseOpened = false,
            UIWindowOpenMode openMode = UIWindowOpenMode.Forward)
        {
            if (string.IsNullOrEmpty(window))
            {
                ServiceDebug.LogError($"Окно для открытия ({nameof(window)}) не назначено");
                return;
            }

            if (openMode == UIWindowOpenMode.Pop)
            {
                if (TryPopToWindow(window)) return;

                CloseAllWindows();
                OpenWindowByID(window, parentWindow, false);
                return;
            }

            if (needToCloseThis && parentWindow != null)
                CloseWindowById(parentWindow.Id.Id);

            if (needToCloseOpened)
                CloseAllWindows();

            OpenWindowByID(window, parentWindow);
        }
        
        /// <summary>
        /// Открытие определенного окна по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор окна</param>
        public void OpenWindowByID(string id)
        {
            OpenWindowByID(id, lastOpenedWindow);
        }

        /// <summary>
        /// Открытие определенного окна по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор окна</param>
        public void OpenWindowByID(string id, UIWindow sourceWindow, bool addSourceToNavigation = true, 
            UIWindowOpenMode openMode = UIWindowOpenMode.Forward)
        {
            if (string.IsNullOrEmpty(id))
            {
                ServiceDebug.LogError($"Передан пустой id в {nameof(OpenWindowByID)}");
                return;
            }

            if (openMode == UIWindowOpenMode.Pop)
            {
                TryPopToWindow(id);
                return;
            }

            UIWindow window = GetOrCreateWindowById(id);

            if (window == null)
            {
                ServiceDebug.LogError($"Окно с id {id} не найдено в {nameof(UIWindowsController)}");
                return;
            }

            UIWindowID previousWindow = ResolvePreviousWindow(sourceWindow);

            window.SetPreviousWindow(previousWindow ? previousWindow : null);

            if (ShouldAddToNavigation(window, previousWindow, addSourceToNavigation))
                navigationStack.Push(previousWindow);

            ActivateWindow(window);
        }

        private void OpenPreviousWindow(UIWindow currentWindow)
        {
            if (currentWindow == null) return;
            if (currentWindow.Type != UIWindowType.window) return;
            if (navigationStack.Count == 0) return;

            UIWindowID previousWindow = navigationStack.Pop();

            if (!previousWindow) return;

            OpenWindowByID(previousWindow.Id, currentWindow, false);
        }

        #endregion

        #region Закрытие окон
        
        /// <summary>
        /// Закрытие текущего окна в фокусе и открытие предыдущего
        /// </summary>
        public void CloseWindow(UIWindow window, bool needToOpenPrevious = true)
        {
            if (window == null) return;

            if (!needToOpenPrevious || window.Type != UIWindowType.window)
            {
                CloseWindowById(window.Id.Id);
                return;
            }

            if (!HasPreviousWindow(window))
            {
                CloseWindowById(window.Id.Id);
                OpenStartWindow();
                return;
            }

            CloseWindowById(window.Id.Id);
            OpenPreviousWindow(window);
        }
        
        /// <summary>
        /// Закрытие определенного окна по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор</param>
        public void CloseWindowById(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            UIWindow window = GetOpenedWindowById(id);

            if (window == null) return;

            CloseOpenedWindow(window);
            activeUIWindows.Remove(window);

            if (lastOpenedWindow == window) UpdateLastOpenedWindow();
        }

        /// <summary>
        /// Закрыть все открытые окна
        /// </summary>
        /// <param name="except">Окно-исключение</param>
        public void CloseAllWindows(UIWindow except = null)
        {
            List<UIWindow> windowsToClose = new(activeUIWindows);

            foreach (UIWindow window in windowsToClose)
            {
                if (except != null && window == except) continue;

                CloseOpenedWindow(window);
                activeUIWindows.Remove(window);
            }

            navigationStack.Clear();
            UpdateLastOpenedWindow();
        }

        #endregion

        #region Внутренние операции

        /// <summary>
        /// Имеет ли окно предыдущее окно (из которого было вызвано)
        /// </summary>
        public bool HasPreviousWindow(UIWindow window)
        {
            if (window == null) return false;
            if (window.Type != UIWindowType.window) return false;

            return navigationStack.Count > 0;
        }
        
        /// <summary>
        /// Открыть стартовое окно
        /// </summary>
        private void OpenStartWindow()
        {
            if (startWindow == null)
            {
                ServiceDebug.LogError($"{nameof(startWindow)} не назначено в {nameof(UIWindowsController)}");
                return;
            }

            OpenWindowByID(startWindow.Id.Id, null);
        }
        
        /// <summary>
        /// Попытаться выполнить переход по стеку навигации к указанному окну
        /// </summary>
        private bool TryPopToWindow(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ServiceDebug.LogError($"Передан пустой id в {nameof(TryPopToWindow)}");
                return false;
            }

            if (!TryTrimNavigationStackTo(id))
            {
                ServiceDebug.LogError($"PopTo: окно {id} не найдено в стеке");
                return false;
            }

            UIWindow targetWindow = GetOrCreateWindowById(id);

            if (targetWindow == null)
            {
                ServiceDebug.LogError($"PopTo: окно {id} не найдено в {nameof(UIWindowsController)}");
                return false;
            }

            List<UIWindow> windowsToClose = new(activeUIWindows);

            foreach (UIWindow window in windowsToClose)
            {
                if (window == targetWindow) continue;

                CloseOpenedWindow(window);
                activeUIWindows.Remove(window);
            }

            ActivateWindow(targetWindow);
            return true;
        }

        /// <summary>
        /// Попытаться обрезать стек навигации до указанного окна
        /// </summary>
        private bool TryTrimNavigationStackTo(string id)
        {
            if (navigationStack.Count == 0) return false;

            Stack<UIWindowID> skippedWindows = new();

            while (navigationStack.Count > 0)
            {
                UIWindowID navigationWindow = navigationStack.Pop();
                skippedWindows.Push(navigationWindow);

                if (!navigationWindow) continue;

                if (navigationWindow.Id == id) return true;
            }

            while (skippedWindows.Count > 0)
                navigationStack.Push(skippedWindows.Pop());

            return false;
        }

        /// <summary>
        /// Получить уже открытое окно или создать его из подготовленного списка
        /// </summary>
        private UIWindow GetOrCreateWindowById(string id)
        {
            UIWindow openedWindow = GetOpenedWindowById(id);
            if (openedWindow != null) return openedWindow;

            UIWindow preparedWindow = GetPreparedWindowById(id);
            if (preparedWindow == null) return null;

            UIWindow instantiatedWindow = Instantiate(preparedWindow.gameObject, root).GetComponent<UIWindow>();
            openedUIWindows.Add(instantiatedWindow);

            return instantiatedWindow;
        }

        /// <summary>
        /// Определить предыдущее окно для навигации
        /// </summary>
        private UIWindowID ResolvePreviousWindow(UIWindow sourceWindow)
        {
            UIWindow navigationSource = sourceWindow;
            if (navigationSource == null) navigationSource = lastOpenedWindow;
            if (navigationSource == null) return null;

            if (navigationSource.Type == UIWindowType.popup) return navigationSource.PreviousWindow;

            return navigationSource.Id;
        }

        /// <summary>
        /// Нужно ли добавлять окно в стек навигации
        /// </summary>
        private bool ShouldAddToNavigation(UIWindow targetWindow, UIWindowID previousWindow, bool addSourceToNavigation)
        {
            if (!addSourceToNavigation) return false;
            if (targetWindow == null) return false;
            if (targetWindow.Type != UIWindowType.window) return false;
            if (!previousWindow) return false;
            if (targetWindow.Id == previousWindow) return false;

            return true;
        }

        /// <summary>
        /// Получить подготовленное окно по идентификатору
        /// </summary>
        private UIWindow GetPreparedWindowById(string id)
        {
            foreach (UIWindow window in preparedUIWindows)
            {
                if (window == null) continue;
                if (window.Id.Id == id) return window;
            }

            return null;
        }

        /// <summary>
        /// Получить открытое окно по идентификатору
        /// </summary>
        private UIWindow GetOpenedWindowById(string id)
        {
            foreach (UIWindow window in openedUIWindows)
            {
                if (window == null) continue;
                if (window.Id.Id == id) return window;
            }

            return null;
        }

        /// <summary>
        /// Активировать окно
        /// </summary>
        private void ActivateWindow(UIWindow window)
        {
            if (window == null) return;

            window.gameObject.SetActive(true);
            window.transform.SetAsLastSibling();

            activeUIWindows.Add(window);
            SetLastOpenedWindow(window);
        }

        /// <summary>
        /// Обновить ссылку на последнее открытое окно
        /// </summary>
        private void UpdateLastOpenedWindow()
        {
            UIWindow topWindow = null;
            int maxSiblingIndex = int.MinValue;

            foreach (UIWindow window in activeUIWindows)
            {
                if (window == null) continue;
                if (!window.gameObject.activeSelf) continue;

                int siblingIndex = window.transform.GetSiblingIndex();

                if (topWindow == null || siblingIndex > maxSiblingIndex)
                {
                    topWindow = window;
                    maxSiblingIndex = siblingIndex;
                }
            }
            
            SetLastOpenedWindow(topWindow);
        }

        /// <summary>
        /// Закрыть открытое окно
        /// </summary>
        private void CloseOpenedWindow(UIWindow window)
        {
            if (window == null) return;

            window.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Установить последнее открытое окно
        /// </summary>
        private void SetLastOpenedWindow(UIWindow window)
        {
            if (lastOpenedWindow == window) return;

            lastOpenedWindow = window;
            onLastOpenedWindowChanged?.Invoke(lastOpenedWindow);
        }

        #endregion
    }
}