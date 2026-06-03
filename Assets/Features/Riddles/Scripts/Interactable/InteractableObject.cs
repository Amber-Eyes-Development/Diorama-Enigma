using System;
using System.Collections.Generic;
using Extensions.Data;
using Extensions.Events;
using Extensions.Log;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Интерактивный объект сцены: клик, дрэг и наведение
    /// </summary>
    public sealed class InteractableObject : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary> Наведение курсора (true — наведён, false — ушёл) </summary>
        public event Action<bool> onHoverChanged;

        /// <summary> Текущее активное состояние </summary>
        public InteractableState State => state;
        /// <summary> ID объекта </summary>
        public string Id => interactableId != null ? interactableId.Id : string.Empty;
        /// <summary> Заблокирован ли объект </summary>
        public bool IsLocked => isLocked;

        #region  Параметры
        
        [Header("Идентификация"), Space]
        [SerializeField] private InteractableID interactableId;

        [Header("Взаимодействие"), Space]
        [Tooltip("Количество состояний, циклично переключаемых по клику")]
        [Min(2)]
        [SerializeField] private int stateCount = 2;
        [SerializeField] private bool isClickable = true;
        [SerializeField] private bool isDraggable = false;

        [Header("Блокировка"), Space]
        [Tooltip("Заблокирован при старте и при сбросе последовательности. " +
                 "Разблокировать через SetInteractableLockEffect в ActivationEffects нужного шага.")]
        [SerializeField] private bool startsLocked = false;

        [Header("Сброс"), Space]
        [Tooltip("Сбросить стейт и блокировку при получении PuzzleSequenceResetEvent")]
        [SerializeField] private bool resetOnSequenceReset = false;

        [Header("Сохранение"), Space]
        [Tooltip("Сохранять и восстанавливать стейт между сессиями. " +
                 "Ключ сохранения — ID из InteractableID-ассета.")]
        [SerializeField] private bool saveState = false;
        
        #endregion

        private InteractableState state;
        private bool isLocked;
        private Camera mainCamera;
        private float dragDepth;
        private EventHub hub;

        #region MonoBehaviour

        private void Awake()
        {
            state = new InteractableState(stateCount);
            isLocked = startsLocked;
            mainCamera = Camera.main;
            hub = RiddleContext.Instance.Hub;

            if (interactableId == null)
            {
                ServiceDebug.LogWarning(this, "interactableId не назначен");
            }
            else if (saveState)
            {
                int savedIndex = JsonSaveLoad.Load<int>(interactableId.Id, defaultValue: 0);
                state.SetSilent(savedIndex);
            }
        }

        private void OnEnable()
        {
            hub.Subscribe<InteractableLockChangedEvent>(OnLockChanged);
            hub.Subscribe<PuzzleSequenceResetEvent>(OnSequenceReset);

            if (saveState && interactableId != null)
                state.onStateChanged += OnStateChangedSave;
        }

        private void OnDisable()
        {
            hub.Unsubscribe<InteractableLockChangedEvent>(OnLockChanged);
            hub.Unsubscribe<PuzzleSequenceResetEvent>(OnSequenceReset);

            if (saveState && interactableId != null)
                state.onStateChanged -= OnStateChangedSave;
        }

        #endregion

        #region Нажатие

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isClickable || isLocked) return;

            state.CycleNext();
            hub.PublishReplay(new InteractableClickedEvent(Id, state.Current));
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
                if (dropZone != null)
                {
                    dropZone.ReceiveDrop(this);
                    return;
                }
            }
        }

        #endregion

        /// <summary>Принять дроп от зоны приземления</summary>
        public void OnDroppedOnZone(DropZoneObject zone)
        {
            if (zone == null)
            {
                ServiceDebug.LogWarning(this, "OnDroppedOnZone вызван с null зоной");
                return;
            }

            hub.Publish(new InteractableDraggedEvent(Id, zone.ZoneId));
        }

        #region Internal

        private void OnLockChanged(InteractableLockChangedEvent evt)
        {
            if (evt.InteractableId == Id)
                isLocked = evt.IsLocked;
        }

        private void OnStateChangedSave(int stateIndex)
        {
            JsonSaveLoad.Save(stateIndex, interactableId.Id);
        }

        private void OnSequenceReset(PuzzleSequenceResetEvent evt)
        {
            if (!resetOnSequenceReset) return;

            state.Reset();
            isLocked = startsLocked;
            hub.ClearReplay<InteractableClickedEvent>();

            if (saveState && interactableId != null)
                JsonSaveLoad.Save(0, interactableId.Id);
        }

        #endregion
    }
}
