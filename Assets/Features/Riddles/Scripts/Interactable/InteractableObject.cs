using System;
using System.Collections.Generic;
using Extensions.Events;
using Extensions.Log;
using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Интерактивный объект сцены: репортит ввод (клик/драг/наведение) и пишет связанные
    /// значения-состояния. О «шагах» не знает — встречается с системой шагов на значении.
    /// </summary>
    public sealed class InteractableObject : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary> Наведение курсора (true — наведён, false — ушёл) </summary>
        public event Action<bool> onHoverChanged;

        /// <summary> ID объекта </summary>
        public string Id => interactableId != null ? interactableId.Id : string.Empty;
        /// <summary> Заблокирован ли объект </summary>
        public bool IsLocked => isLocked;

        #region Параметры

        [Header("Идентификация"), Space]
        [SerializeField] private InteractableID interactableId;

        [Header("Клик"), Space]
        [SerializeField] private bool isClickable = true;
        [Tooltip("Значение-состояние, циклически переключаемое по клику (например StateSetPuzzleStep). Опционально")]
        [SerializeField] private IntValue clickState;
        [Tooltip("Количество циклически переключаемых состояний")]
        [Min(2)]
        [SerializeField] private int stateCount = 2;

        [Header("Перетаскивание"), Space]
        [SerializeField] private bool isDraggable = false;
        [Tooltip("Значение-состояние, выставляемое в true при дропе в нужную зону (например BoolPuzzleStep). Опционально")]
        [SerializeField] private BoolValue dropState;
        [Tooltip("Зона, в которую нужно перетащить (пусто — любая зона)")]
        [SerializeField] private InteractableID requiredDropZone;

        [Header("Блокировка"), Space]
        [Tooltip("Заблокирован при старте и при сбросе последовательности. " +
                 "Разблокировать через SetInteractableLockEffect в ActivationEffects нужного шага")]
        [SerializeField] private bool startsLocked = false;

        #endregion

        private bool isLocked;
        private Camera mainCamera;
        private float dragDepth;
        private EventHub hub;

        #region MonoBehaviour

        private void Awake()
        {
            isLocked = startsLocked;
            mainCamera = Camera.main;

            RiddleContext context = GetComponentInParent<RiddleContext>();
            if (context == null)
                ServiceDebug.LogWarning(this, "RiddleContext не найден в родителях");
            else
                hub = context.Hub;

            if (interactableId == null)
                ServiceDebug.LogWarning(this, "interactableId не назначен");
        }

        private void OnEnable()
        {
            if (hub == null) return;

            hub.Subscribe<InteractableLockChangedEvent>(OnLockChanged);
            hub.Subscribe<PuzzleSequenceResetEvent>(OnSequenceReset);
        }

        private void OnDisable()
        {
            if (hub == null) return;

            hub.Unsubscribe<InteractableLockChangedEvent>(OnLockChanged);
            hub.Unsubscribe<PuzzleSequenceResetEvent>(OnSequenceReset);
        }

        #endregion

        #region Нажатие

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isClickable || isLocked || clickState == null) return;

            int next = stateCount > 0 ? (clickState.Value + 1) % stateCount : clickState.Value + 1;
            clickState.SetValue(next);
        }

        #endregion

        #region Hover

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked) return;

            onHoverChanged?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isLocked) return;

            onHoverChanged?.Invoke(false);
        }

        #endregion

        #region Перетаскивание

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || isLocked) return;

            if (mainCamera == null) mainCamera = Camera.main;
            dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable || isLocked) return;

            if (mainCamera == null) mainCamera = Camera.main;

            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, dragDepth);
            transform.position = mainCamera.ScreenToWorldPoint(screenPos);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable || isLocked) return;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                var dropZone = result.gameObject.GetComponent<DropZoneObject>();
                if (dropZone == null) continue;

                dropZone.ReceiveDrop(this);
                return;
            }
        }

        /// <summary> Принять дроп от зоны приземления </summary>
        public void OnDroppedOnZone(DropZoneObject zone)
        {
            if (zone == null)
            {
                ServiceDebug.LogWarning(this, "OnDroppedOnZone вызван с null зоной");
                return;
            }

            if (requiredDropZone != null && zone.ZoneId != requiredDropZone.Id) return;
            if (dropState != null) dropState.SetValue(true);
        }

        #endregion

        #region Internal

        private void OnLockChanged(InteractableLockChangedEvent evt)
        {
            if (evt.InteractableId == Id) isLocked = evt.IsLocked;
        }

        private void OnSequenceReset(PuzzleSequenceResetEvent evt) => isLocked = startsLocked;

        #endregion
    }
}
