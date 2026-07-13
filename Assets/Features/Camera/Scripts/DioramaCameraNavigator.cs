using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using Extensions.Log;
using Extensions.ScriptableValues;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DioramaEnigma.CameraUtils
{
    /// <summary>
    /// Осмотр диорамы в фокусе: панорамирование ЛКМ по плоскости наблюдения и зум колесом мыши
    /// </summary>
    /// <remarks>
    /// Живёт на пивоте камеры рядом с <see cref="DioramaCameraController"/>: контроллер двигает мировую позицию
    /// пивота (кадрирование), навигатор — только локальный сдвиг дочерней ортокамеры (пан) и её orthographicSize (зум),
    /// поэтому системы не конфликтуют. Пределы глобальные, с учётом текущего зума. Пан/зум запоминаются по диораме;
    /// сброс зума при смене фокуса — по флагу <see cref="resetZoomOnFocusChange"/>. Пан не стартует над draggable/UI,
    /// чтобы не конфликтовать с <see cref="Sequences.DraggableInteractable"/>
    /// </remarks>
    public sealed class DioramaCameraNavigator : MonoBehaviour
    {
        #region Параметры

        [Header("Цель"), Space]
        [Tooltip("Дочерняя ортокамера: пан — её localPosition в плоскости XY пивота, зум — orthographicSize")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Канал рантайм-ссылки на спавнер диорам — источник событий смены фокуса")]
        [SerializeField] private DioramaSpawnerReference spawnerReference;

        [Header("Ввод"), Space]
        [Tooltip("Кнопка захвата вида (ЛКМ)")]
        [SerializeField] private InputActionReference dragButton;
        [Tooltip("Позиция курсора (Vector2)")]
        [SerializeField] private InputActionReference pointerPosition;
        [Tooltip("Колесо мыши (Vector2, берётся Y)")]
        [SerializeField] private InputActionReference zoomScroll;

        [Header("Панорамирование"), Space]
        [Tooltip("Плавность ведения вида, сек (0 — мгновенно)")]
        [Min(0f)]
        [SerializeField] private float panSmoothTime = 0.08f;
        [Tooltip("Область панорамирования как доля базового (полностью отдалённого) кадра: 1 — ровно базовый кадр (за него не выходим), >1 — можно чуть дальше, <1 — уже. Место для пана открывается по мере приближения")]
        [Min(0f)]
        [SerializeField] private float panAreaScale = 1f;
        [Tooltip("Смещение центра области панорамирования относительно точки кадрирования (тюнинг)")]
        [SerializeField] private Vector2 centerOffset = Vector2.zero;

        [Header("Зум"), Space]
        [Tooltip("Минимальный orthographicSize (максимальное приближение)")]
        [Min(0.01f)]
        [SerializeField] private float minZoom = 1f;
        [Tooltip("Максимальный orthographicSize (максимальное отдаление)")]
        [Min(0.01f)]
        [SerializeField] private float maxZoom = 3f;
        [Tooltip("Изменение orthographicSize за один щелчок колеса")]
        [Min(0f)]
        [SerializeField] private float zoomStep = 0.35f;
        [Tooltip("Длительность плавного доезда зума, сек")]
        [Min(0f)]
        [SerializeField] private float zoomDuration = 0.2f;
        [Tooltip("Кривая сглаживания доезда зума (0→1)")]
        [SerializeField] private AnimationCurve zoomEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Уровень следования зума за курсором: 0 — к центру вида (обычное), 1 — центр моментально переносится на точку под курсором (с учётом рамок). Промежуточные — частичное подтягивание за щелчок")]
        [Range(0f, 1f)]
        [SerializeField] private float cursorFollow = 0.5f;

        [Header("Объём (наклон по смещению)"), Space]
        [Tooltip("Включить лёгкий поворот камеры вокруг пивота по смещению — ощущение объёма (параллакс)")]
        [SerializeField] private bool tiltEnabled = true;
        [Tooltip("Градусов на 1 мир. ед. смещения от центра: X → рыскание (гориз. пан), Y → тангаж (верт. пан). Знак задаёт сторону")]
        [SerializeField] private Vector2 tiltStrength = new(-3f, 3f);
        [Tooltip("Предел угла наклона по каждой оси, град")]
        [Min(0f)]
        [SerializeField] private float maxTiltAngle = 6f;
        [Tooltip("Коэффициент наклона по приближению: аргумент 0 — макс. отдаление (maxZoom), 1 — макс. приближение (minZoom). Дефолт линейный: на отдалении наклона нет, на приближении — полный")]
        [SerializeField] private AnimationCurve tiltZoomInfluence = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Смена фокуса"), Space]
        [Tooltip("Сбрасывать зум к дефолтному при переходе к другой диораме (иначе — восстанавливать запомненный)")]
        [SerializeField] private BoolValue resetZoomOnFocusChange;

        [Header("UI"), Space]
        [Tooltip("Опционально. Канал уровня приближения для UI: 1.0 = 100% (вся диорама видна на maxZoom), растёт при зуме. Слайдер/текст читают его готовыми биндерами (FloatValueSliderBinder / FloatValueView с showAsPercent)")]
        [SerializeField] private FloatValue zoomLevel;

        #endregion

        #region Переменные

        private DioramaSpawner spawner;
        private float baseOrthographicSize;
        
        private Vector2 targetPan;
        private Vector2 pan;
        private Vector2 panVelocity;
        private bool panning;
        private Vector2 lastPointerPos;

        private Vector2 panFrom;
        private Vector2 panTo;
        private bool zoomPanning;

        private float targetZoom;
        private float zoomFrom;
        private float zoomTimer;

        private string currentId;
        private readonly Dictionary<string, PanZoomState> memory = new();
        private readonly List<RaycastResult> raycastBuffer = new();

        #endregion

        private struct PanZoomState
        {
            public Vector2 pan;
            public float zoom;
        }

        private bool ResetZoom => resetZoomOnFocusChange != null && resetZoomOnFocusChange.Value;

        #region MonoBehaviour

        private void OnEnable()
        {
            if (!ValidateRefs())
            {
                enabled = false;
                return;
            }

            InitState();
            SubscribeSpawner();
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
            UnsubscribeSpawner();
        }

        private void Update()
        {
            UpdateZoom();
            UpdatePan();
        }

        #endregion

        #region Ввод

        private void EnableInput()
        {
            if (dragButton != null && dragButton.action != null)
            {
                dragButton.action.started += OnDragStarted;
                dragButton.action.canceled += OnDragCanceled;
                dragButton.action.Enable();
            }

            if (zoomScroll != null && zoomScroll.action != null)
            {
                zoomScroll.action.performed += OnZoomPerformed;
                zoomScroll.action.Enable();
            }

            if (pointerPosition != null && pointerPosition.action != null)
                pointerPosition.action.Enable();
        }

        private void DisableInput()
        {
            if (dragButton != null && dragButton.action != null)
            {
                dragButton.action.started -= OnDragStarted;
                dragButton.action.canceled -= OnDragCanceled;
                dragButton.action.Disable();
            }

            if (zoomScroll != null && zoomScroll.action != null)
            {
                zoomScroll.action.performed -= OnZoomPerformed;
                zoomScroll.action.Disable();
            }

            if (pointerPosition != null && pointerPosition.action != null)
                pointerPosition.action.Disable();

            panning = false;
        }

        private void OnDragStarted(InputAction.CallbackContext context)
        {
            if (PointerBlocksPan()) return;

            panning = true;
            zoomPanning = false;
            panVelocity = Vector2.zero;
            lastPointerPos = ReadPointer();
        }

        private void OnDragCanceled(InputAction.CallbackContext context) => panning = false;

        private void OnZoomPerformed(InputAction.CallbackContext context)
        {
            float scrollY = context.ReadValue<Vector2>().y;
            if (Mathf.Approximately(scrollY, 0f)) return;

            targetZoom = Mathf.Clamp(
                targetZoom + Mathf.Sign(scrollY) * zoomStep,
                minZoom,
                maxZoom);

            RestartZoomTween();

            if (cursorFollow > 0f) BeginCursorZoom();
        }

        private void BeginCursorZoom()
        {
            if (Screen.height <= 0) return;

            Vector2 fromCenter = ReadPointer() - new Vector2(Screen.width, Screen.height) * 0.5f;
            float worldPerPixel = 2f * targetCamera.orthographicSize / Screen.height;
            Vector2 cursorWorld = pan + fromCenter * worldPerPixel;

            panFrom = pan;
            panTo = ClampPan(Vector2.Lerp(pan, cursorWorld, cursorFollow), TargetSize());
            targetPan = panTo;
            zoomPanning = true;
        }

        #endregion

        #region Пан

        private void UpdatePan()
        {
            if (panning)
            {
                Vector2 pointer = ReadPointer();
                Vector2 delta = pointer - lastPointerPos;
                lastPointerPos = pointer;

                float worldPerPixel = Screen.height > 0 ? 2f * targetCamera.orthographicSize / Screen.height : 0f;

                targetPan = ClampPan(targetPan - delta * worldPerPixel);
                zoomPanning = false; 
            }

            if (!zoomPanning)
            {
                pan = Vector2.SmoothDamp(pan, targetPan, ref panVelocity, panSmoothTime);
                pan = ClampPan(pan, targetCamera.orthographicSize);
            }

            ApplyCameraTransform();
        }

        private void ApplyCameraTransform()
        {
            Vector3 localPan = new(pan.x, pan.y, 0f);
            Quaternion tilt = Quaternion.identity;

            if (tiltEnabled)
            {
                Vector2 offset = pan - centerOffset;
                float factor = tiltZoomInfluence.Evaluate(ZoomInNormalized());

                float yaw = Mathf.Clamp(offset.x * tiltStrength.x, -maxTiltAngle, maxTiltAngle) * factor;
                float pitch = Mathf.Clamp(offset.y * tiltStrength.y, -maxTiltAngle, maxTiltAngle) * factor;
                tilt = Quaternion.Euler(pitch, yaw, 0f);
            }

            targetCamera.transform.localRotation = tilt;
            targetCamera.transform.localPosition = tilt * localPan;
        }

        private float ZoomInNormalized()
        {
            if (Mathf.Approximately(maxZoom, minZoom))
                return 1f;

            return Mathf.InverseLerp(minZoom, maxZoom, CurrentZoom());
        }

        private bool PointerBlocksPan()
        {
            if (EventSystem.current == null) return false;

            var pointerData = new PointerEventData(EventSystem.current) { position = ReadPointer() };
            raycastBuffer.Clear();
            EventSystem.current.RaycastAll(pointerData, raycastBuffer);

            if (raycastBuffer.Count == 0) return false;

            RaycastResult top = raycastBuffer[0];
            if (top.module is GraphicRaycaster) return true;
            return top.gameObject != null && top.gameObject.GetComponentInParent<IBeginDragHandler>() != null;
        }

        private Vector2 ClampPan(Vector2 value) => ClampPan(value, TargetSize());

        private Vector2 ClampPan(Vector2 value, float size)
        {
            float limitY = Mathf.Max(0f, baseOrthographicSize * panAreaScale - size);
            float limitX = limitY * targetCamera.aspect;

            return new Vector2(
                Mathf.Clamp(value.x, centerOffset.x - limitX, centerOffset.x + limitX),
                Mathf.Clamp(value.y, centerOffset.y - limitY, centerOffset.y + limitY));
        }

        private float TargetSize() => baseOrthographicSize / Mathf.Max(targetZoom, 0.0001f);

        private Vector2 ReadPointer()
        {
            if (pointerPosition != null && pointerPosition.action != null)
                return pointerPosition.action.ReadValue<Vector2>();

            return lastPointerPos;
        }

        #endregion

        #region Зум

        private void UpdateZoom()
        {
            zoomTimer += Time.deltaTime;
            float t = zoomDuration > 0f ? Mathf.Clamp01(zoomTimer / zoomDuration) : 1f;
            float eased = zoomEase.Evaluate(t);

            float currentZoom = Mathf.Lerp(zoomFrom, targetZoom, eased);
            targetCamera.orthographicSize = baseOrthographicSize / currentZoom;
            PublishZoom();

            if (zoomPanning)
            {
                targetPan = panTo;
                pan = ClampPan(Vector2.Lerp(panFrom, panTo, eased), targetCamera.orthographicSize);
                if (t >= 1f) zoomPanning = false;
            }
            else
            {
                targetPan = ClampPan(targetPan);
            }
        }

        private void PublishZoom()
        {
            if (zoomLevel == null) return;

            zoomLevel.SetValue(CurrentZoom());
        }

        private float CurrentZoom()
        {
            if (targetCamera.orthographicSize <= 0f)
                return minZoom;

            return baseOrthographicSize / targetCamera.orthographicSize;
        }

        private void RestartZoomTween()
        {
            zoomFrom = CurrentZoom();
            zoomTimer = 0f;
        }

        #endregion

        #region Фокус

        private void SubscribeSpawner()
        {
            spawnerReference.onInitialized += AttachSpawner;
            spawnerReference.onReleased += DetachSpawner;

            if (spawnerReference.HasValue) AttachSpawner(spawnerReference.Current);
        }

        private void UnsubscribeSpawner()
        {
            if (spawnerReference != null)
            {
                spawnerReference.onInitialized -= AttachSpawner;
                spawnerReference.onReleased -= DetachSpawner;
            }

            DetachSpawner();
        }

        private void AttachSpawner(DioramaSpawner value)
        {
            if (spawner == value) return;

            DetachSpawner();
            spawner = value;
            spawner.onFocusChanged += OnFocusChanged;
            currentId = spawner.Active != null ? spawner.Active.Id : null;
        }

        private void DetachSpawner()
        {
            if (spawner == null) return;

            spawner.onFocusChanged -= OnFocusChanged;
            spawner = null;
        }

        private void OnFocusChanged(DioramaFocus focus)
        {
            string newId = spawner != null && spawner.Active != null ? spawner.Active.Id : null;
            if (newId == currentId) return;

            if (currentId != null)
                memory[currentId] = new PanZoomState { pan = targetPan, zoom = targetZoom };

            if (newId != null && memory.TryGetValue(newId, out PanZoomState saved))
            {
                targetPan = saved.pan;
                targetZoom = ResetZoom ? DefaultZoom() : saved.zoom;
            }
            else
            {
                targetPan = centerOffset;
                targetZoom = DefaultZoom();
            }

            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            targetPan = ClampPan(targetPan);
            RestartZoomTween();
            currentId = newId;
        }

        #endregion

        #region Инициализация

        private bool ValidateRefs()
        {
            if (targetCamera == null)
            {
                ServiceDebug.LogError($"{nameof(targetCamera)} не назначен");
                return false;
            }

            if (spawnerReference == null)
            {
                ServiceDebug.LogError($"{nameof(spawnerReference)} не назначен");
                return false;
            }

            if (dragButton == null || pointerPosition == null || zoomScroll == null)
            {
                ServiceDebug.LogError("Не назначены действия ввода (dragButton / pointerPosition / zoomScroll)");
                return false;
            }

            return true;
        }

        private void InitState()
        {
            baseOrthographicSize = targetCamera.orthographicSize;

            targetZoom = DefaultZoom();
            zoomFrom = targetZoom;
            zoomTimer = zoomDuration;

            targetCamera.orthographicSize = baseOrthographicSize / targetZoom;
            PublishZoom();

            targetPan = ClampPan(centerOffset);
            pan = targetPan;
            panVelocity = Vector2.zero;
            ApplyCameraTransform();
        }
        
        private float DefaultZoom() => Mathf.Clamp(1f, minZoom, maxZoom);

        #endregion
    }
}
