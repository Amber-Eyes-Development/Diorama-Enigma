using System;
using Extensions.Log;
using UnityEngine.InputSystem;
using Extensions.Data;
using Extensions.Singleton;
using UnityEngine;

namespace Extensions.Input
{
    /// <summary>
    /// Контроллер пользовательских привязок ввода
    /// </summary>
    public sealed class BindingController : MonoBehaviourSingleton<BindingController>
    {
        private const string SAVE_KEY = "input-bindings";

        #region События
        /// <summary>
        /// Событие начала изменения привязки
        /// </summary>
        public event Action<InputActionReference, int> onRebindStarted;
        /// <summary>
        /// Событие отмены изменения привязки
        /// </summary>
        public event Action<InputActionReference, int> onRebindCanceled;
        /// <summary>
        /// Событие завершения изменения привязки
        /// </summary>
        public event Action<InputActionReference, int> onBindingChanged;
        #endregion

        /// <summary>
        /// Текст ожидания ввода новой привязки
        /// </summary>
        public string RebindingWaitingText => rebindingWaitingText;
        
        #region Параметры
        [Header("Ввод"), Space]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private string rebindingWaitingText = "...";
        #endregion

        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        
        protected override void Awake()
        {
            base.Awake();
            if (loadOnAwake) LoadBindings();
        }
        
        #region Применение и сброс привязки

        /// <summary>
        /// Применить привязку к действию
        /// </summary>
        public void ApplyBinding(InputActionReference inputActionReference, int bindingIndex, string bindingPath)
        {
            if (!ValidateInputAction(inputActionReference)) return;

            if (string.IsNullOrEmpty(bindingPath))
            {
                ServiceDebug.LogError($"Пустой путь привязки для {inputActionReference.action.name}");
                return;
            }

            inputActionReference.action.ApplyBindingOverride(bindingIndex, bindingPath);
            SaveBindings();
        }

        /// <summary>
        /// Сбросить привязку действия
        /// </summary>
        public void ResetBinding(InputActionReference inputActionReference, int bindingIndex)
        {
            if (!ValidateInputAction(inputActionReference)) return;

            inputActionReference.action.RemoveBindingOverride(bindingIndex);
            SaveBindings();
            onBindingChanged?.Invoke(inputActionReference, bindingIndex);
        }

        /// <summary>
        /// Сбросить все привязки
        /// </summary>
        public void ResetAllBindings()
        {
            if (inputActions == null)
            {
                ServiceDebug.LogError($"{nameof(inputActions)} не назначен в {nameof(BindingController)}");
                return;
            }

            inputActions.RemoveAllBindingOverrides();
            SaveBindings();
        }
        
        #endregion

        #region Сохранение и загрузка привязок
        
        /// <summary>
        /// Сохранить привязки
        /// </summary>
        public void SaveBindings()
        {
            if (inputActions == null)
            {
                ServiceDebug.LogError($"{nameof(inputActions)} не назначен в {nameof(BindingController)}");
                return;
            }

            string json = inputActions.SaveBindingOverridesAsJson();
            JsonSaveLoad.Save(json, SAVE_KEY);
        }

        /// <summary>
        /// Загрузить привязки
        /// </summary>
        public void LoadBindings()
        {
            if (inputActions == null)
            {
                ServiceDebug.LogError($"{nameof(inputActions)} не назначен в {nameof(BindingController)}");
                return;
            }

            string json = JsonSaveLoad.Load(SAVE_KEY, string.Empty);

            if (string.IsNullOrEmpty(json))
                return;

            inputActions.LoadBindingOverridesFromJson(json);
        }

        #endregion
        
        #region Отображение
        
        /// <summary>
        /// Получить отображаемое имя привязки
        /// </summary>
        public string GetBindingDisplayString(InputActionReference inputActionReference, int bindingIndex)
        {
            if (!ValidateInputAction(inputActionReference)) return string.Empty;

            if (bindingIndex < 0 || bindingIndex >= inputActionReference.action.bindings.Count)
            {
                ServiceDebug.LogError($"Индекс привязки {bindingIndex} вне диапазона для {inputActionReference.action.name}");
                return string.Empty;
            }

            return inputActionReference.action.GetBindingDisplayString(bindingIndex);
        }
        
        #endregion

        #region Процесс изменения привязки
        
        /// <summary>
        /// Начать интерактивное изменение привязки
        /// </summary>
        public void StartInteractiveRebind(InputActionReference inputActionReference, int bindingIndex)
        {
            if (!ValidateInputAction(inputActionReference)) return;

            if (bindingIndex < 0 || bindingIndex >= inputActionReference.action.bindings.Count)
            {
                ServiceDebug.LogError($"Индекс привязки {bindingIndex} вне диапазона для {inputActionReference.action.name}");
                return;
            }

            DisposeRebindingOperation();

            InputAction inputAction = inputActionReference.action;
            inputAction.Disable();

            onRebindStarted?.Invoke(inputActionReference, bindingIndex);

            rebindingOperation = inputAction
                .PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    rebindingOperation = null;

                    inputAction.Enable();

                    onRebindCanceled?.Invoke(inputActionReference, bindingIndex);
                    onBindingChanged?.Invoke(inputActionReference, bindingIndex);
                })
                .OnComplete(operation =>
                {
                    operation.Dispose();
                    rebindingOperation = null;

                    inputAction.Enable();

                    SaveBindings();

                    onBindingChanged?.Invoke(inputActionReference, bindingIndex);
                });

            rebindingOperation.Start();
        }

        private void DisposeRebindingOperation()
        {
            if (rebindingOperation == null) return;

            rebindingOperation.Dispose();
            rebindingOperation = null;
        }

        #endregion

        #region Внутренняя логика

        private bool ValidateInputAction(InputActionReference inputActionReference)
        {
            if (inputActionReference == null)
            {
                ServiceDebug.LogError($"{nameof(inputActionReference)} не назначен");
                return false;
            }

            if (inputActionReference.action == null)
            {
                ServiceDebug.LogError($"Действие ввода не назначено в {nameof(inputActionReference)}");
                return false;
            }

            return true;
        }

        #endregion
    }
}