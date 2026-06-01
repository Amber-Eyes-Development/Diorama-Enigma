using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Extensions.Coroutines;
using Extensions.ScriptableValues;
using UnityEngine.EventSystems;

namespace Extensions.Generics
{
    /// <summary>
    /// Оркестратор очередности действий кнопок <see cref="AbstractHoldButtonAction"/>
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class HoldButtonClickOrchestrator : BaseButtonOrchestrator,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float DEFAULT_HOLD_DURATION = 1f;
        
        #region События
        
        /// <summary>
        /// Зажатие кнопки начато
        /// </summary>
        public event Action onHoldStarted;
        /// <summary>
        /// Измнение прогресса зажатия
        /// </summary>
        /// <typeparam name="float">Прогресс зажатия от 0 до 1</typeparam>
        public event Action<float> onHoldProgressChanged;
        /// <summary>
        /// Кнопка отжата до завершения (выполнения onButtonClicked)
        /// </summary>
        public event Action onHoldCanceled;
        /// <summary>
        /// Кнопка зажата до конца
        /// </summary>
        public event Action onHoldCompleted;

        #endregion

        #region Параметры
        
        [Header("Параметры удержания"), Space]
        [SerializeField] private FloatValue holdDurationValue;
        [SerializeField] private bool useUnscaledTime = true;
        
        #endregion
        
        #region Переменные

        private CoroutineTask holdTask;

        private bool isHolding;
        private float holdDuration;
        private float holdTime;
        
        #endregion
        
        #region MonoBehaviour
        
        private void Awake()
        {
            button = GetComponent<Button>();
            holdTask = new CoroutineTask(this);
        }
        
        private void OnEnable()
        {
            holdDuration = holdDurationValue == null ? DEFAULT_HOLD_DURATION : holdDurationValue.Value;
            if (holdDuration < 0f) holdDuration = 0f;
        }

        private void OnDisable()
        {
            if (isHolding) CancelHoldInternal(true);
            else holdTask?.Stop();
        }
        
        #endregion
        
        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;
            // if (eventData.button != PointerEventData.InputButton.Left) return; // TODO заменить на настраиваемую конфигурацию?

            StartHold();
        }

        public void OnPointerUp(PointerEventData eventData) => TryCancelHold();

        public void OnPointerExit(PointerEventData eventData) => TryCancelHold();
        
        #endregion

        #region Обработка действий
        
        /// <summary>
        /// Добавить действие для выполнения по нажатию
        /// </summary>
        public void AddAction(AbstractHoldButtonAction action)
        {
            if (action != null) actions.Add(action);
            sortActionsByPriority();
        }

        /// <summary>
        /// Удалить действие для выполнения по нажатию
        /// </summary>
        public void RemoveAction(AbstractHoldButtonAction action)
        {
            if (action != null) actions.Remove(action);
        }
        
        #endregion
        
        #region Зажатие кнопки

        /// <summary>
        /// Прогресс зажатия до выполнения действия (0f-1f)
        /// </summary>
        public float GetProgress()
        {
            if (holdDuration <= 0f) return 1f;
            return Mathf.Clamp01(holdTime / holdDuration);
        }
        
        private void StartHold()
        {
            if (holdTask == null || holdTask.IsRunning) return;

            isHolding = true;
            holdTime = 0f;

            onHoldStarted?.Invoke();
            onHoldProgressChanged?.Invoke(0f);

            if (GetProgress() >= 1f)
            {
                CompleteHoldInternal();
                return;
            }
            
            holdTask.Start(HoldRoutine());
        }

        private IEnumerator HoldRoutine()
        {
            while (isHolding)
            {
                if (button == null || !button.interactable)
                {
                    CancelHoldInternal(true);
                    yield break;
                }

                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                holdTime += dt;

                float progress = GetProgress();
                onHoldProgressChanged?.Invoke(progress);

                if (progress >= 1f)
                {
                    CompleteHoldInternal();
                    yield break;
                }

                yield return null;
            }
        }

        private void TryCancelHold()
        {
            if (!isHolding) return;
            CancelHoldInternal(false);
        }

        private void CancelHoldInternal(bool silent)
        {
            isHolding = false;
            holdTime = 0f;

            holdTask?.Stop();
            onHoldProgressChanged?.Invoke(0f);

            if (!silent) onHoldCanceled?.Invoke();
        }

        private void CompleteHoldInternal()
        {
            isHolding = false;
            holdTime = 0f;

            holdTask?.Stop();
            onHoldProgressChanged?.Invoke(1f);

            onHoldCompleted?.Invoke();

            ProcessActions();
        }
        
        #endregion
    }
}