using System;
using DioramaEnigma.Sequences;
using Extensions.Attributes;
using Extensions.Audio;
using Extensions.Data;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace DioramaEnigma.Scratch
{
    /// <summary>
    /// Интерактивный стираемый слой
    /// </summary>
    /// <remarks>
    /// Если на объекте есть <see cref="StepReference"/> — стирание блокируется условиями шага
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ScratchSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string MASK_PROPERTY = "_ScratchMask";
        private const string SAVE_KEY_POSTFIX = "_scratch_mask";
        private const int MIN_RESOLUTION = 8;
        private const byte PRESENT_THRESHOLD = 128;

        /// <summary> Изменение доли стирания (0..1) </summary>
        public event Action<float> onProgressChanged;

        /// <summary> Доля стёртого слоя в диапазоне 0..1 </summary>
        public float Progress => totalTexels == 0 ? 0f : clearedTexels / (float)totalTexels;

        [Header("Слой стирания"), Space]
        [Tooltip("Режим маппинга кисти: проекция декали или реальный UV поверхности")]
        [SerializeField] private ScratchProjection projection;
        [Tooltip("Декаль-проектор (режим DecalProjector): его материал клонируется, в инстанс пишется маска _ScratchMask")]
        [ShowIf(nameof(projection), ScratchProjection.DecalProjector)]
        [SerializeField] private DecalProjector dustDecal;
        [Tooltip("Рендерер меша (режим SurfaceUv): его материал клонируется, в инстанс пишется маска _ScratchMask")]
        [ShowIf(nameof(projection), ScratchProjection.SurfaceUv)]
        [SerializeField] private Renderer dustRenderer;
        [Tooltip("Разрешение маски стирания (квадрат)")]
        [Min(MIN_RESOLUTION)]
        [SerializeField] private int maskResolution = 256;
        [Tooltip("Цвет «слой на месте» (для шейдера важен только R-канал)")]
        [SerializeField] private Color dustPresent = Color.white;

        [Header("Стирание"), Space]
        [Tooltip("Слои поверхностей, по которым ловим точку трения")]
        [SerializeField] private LayerMask surfaceMask = ~0;
        [Tooltip("Радиус кисти стирания в долях UV (0..1)")]
        [Range(0.005f, 0.5f)]
        [SerializeField] private float brushRadiusUv = 0.06f;

        [Tooltip("Декаль-режим: зеркалить ось U")]
        [ShowIf(nameof(projection), ScratchProjection.DecalProjector)]
        [SerializeField] private bool flipU;
        [Tooltip("Декаль-режим: зеркалить ось V")]
        [ShowIf(nameof(projection), ScratchProjection.DecalProjector)]
        [SerializeField] private bool flipV;
        [Tooltip("Декаль-режим: поменять U и V местами")]
        [ShowIf(nameof(projection), ScratchProjection.DecalProjector)]
        [SerializeField] private bool swapUV;

        [Header("Фидбэк (ведётся за курсором)"), Space]
        [Tooltip("Эмиттер пыли (Rate over Distance): двигается за точкой контакта")]
        [SerializeField] private ParticleSystem dustParticles;
        [Tooltip("Звук трения: один зацикленный источник, ведётся за курсором")]
        [SerializeField] private AudioResource scratchSound;

        [Header("Сохранение"), Space]
        [Tooltip("Сохранять маску стирания между сессиями")]
        [SerializeField] private bool saveProgress = true;
        [Tooltip("Уникальный ключ сохранения маски (заполняется автоматически)")]
        [SerializeField] private string saveId;

        private static readonly Color32 Erased = new(0, 0, 0, 0);

        [Serializable]
        private struct ScratchSave
        {
            public byte[] mask;
        }

        private Texture2D mask;
        private Material materialInstance;
        private Color32[] pixels;
        private int clearedTexels;
        private int totalTexels;
        private bool dirty;
        private bool saveDirty;

        private Camera mainCamera;
        private Collider surfaceCollider;
        private SequenceStep step;

        private AudioSource loopSource;
        private bool feedbackActive;

        private string SaveKey => string.IsNullOrEmpty(saveId) ? null : saveId + SAVE_KEY_POSTFIX;
        private bool IsScratchAllowed => step == null || step.CanChangeValue;

        #region Unity lifecycle

        private void Awake()
        {
            mainCamera = Camera.main;
            step = GetComponent<StepReference>()?.Step as SequenceStep;

            Material source = ResolveMaterialSource();
            if (source == null)
            {
                enabled = false;
                return;
            }

            if (projection == ScratchProjection.SurfaceUv && dustRenderer != null)
                surfaceCollider = dustRenderer.GetComponent<Collider>();

            BuildMask();
            if (step == null || step.IsCompleted) LoadSavedMask();

            materialInstance = new Material(source);
            materialInstance.SetTexture(MASK_PROPERTY, mask);
            AssignMaterial(materialInstance);
        }

        private void OnEnable()
        {
            if (step != null) step.onCompletionChanged += OnStepCompletionChanged;
        }

        private void OnDisable()
        {
            if (step != null) step.onCompletionChanged -= OnStepCompletionChanged;

            StopFeedback();
            SaveMask();
        }

        private void LateUpdate()
        {
            if (!dirty) return;

            mask.SetPixels32(pixels);
            mask.Apply(false);
            dirty = false;
        }

        private void OnDestroy()
        {
            if (materialInstance != null) Destroy(materialInstance);
            if (mask != null) Destroy(mask);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(saveId)) return;

            saveId = IdGenerator.NewGuid();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion

        #region Ввод

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Шаг недоступен (нет ресурса/залочен/кулдаун) — отклоняем попытку с фидбэком
            if (!IsScratchAllowed && step != null) step.NotifyInteractionRejected();
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            if (pixels == null) return;

            if (!IsScratchAllowed)
            {
                StopFeedback();
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(eventData.position);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                StopFeedback();
                return;
            }

            if (!TryResolveUv(hit, out Vector2 uv))
            {
                StopFeedback();
                return;
            }

            UpdateFeedback(hit.point, hit.normal);
            EraseAtUv(uv, brushRadiusUv);
        }

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData eventData) => StopFeedback();

        #endregion

        #region Стирание

        /// <summary> Полностью стереть слой (завершение) либо вернуть его целиком (re-fog/сброс) </summary>
        /// <param name="full">true — поверхность чистая; false — слой на месте</param>
        public void SetErased(bool full)
        {
            if (pixels == null) return;

            Color32 fill = full ? Erased : (Color32)dustPresent;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;

            dirty = true;
            saveDirty = true;

            int newCleared = full ? totalTexels : 0;
            if (newCleared == clearedTexels) return;

            clearedTexels = newCleared;
            onProgressChanged?.Invoke(Progress);
        }

        /// <summary> Стереть кистью круг по готовому UV маски </summary>
        private void EraseAtUv(Vector2 uv, float radiusUv)
        {
            int centerX = Mathf.RoundToInt(uv.x * (maskResolution - 1));
            int centerY = Mathf.RoundToInt(uv.y * (maskResolution - 1));
            int radius = Mathf.Max(1, Mathf.RoundToInt(radiusUv * maskResolution));
            int radiusSqr = radius * radius;

            int minX = Mathf.Max(0, centerX - radius);
            int maxX = Mathf.Min(maskResolution - 1, centerX + radius);
            int minY = Mathf.Max(0, centerY - radius);
            int maxY = Mathf.Min(maskResolution - 1, centerY + radius);

            bool changed = false;
            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    if (dx * dx + dy * dy > radiusSqr) continue;

                    int index = y * maskResolution + x;
                    if (pixels[index].r < PRESENT_THRESHOLD) continue;

                    pixels[index] = Erased;
                    clearedTexels++;
                    changed = true;
                }
            }

            if (!changed) return;

            dirty = true;
            saveDirty = true;
            onProgressChanged?.Invoke(Progress);
        }

        private bool TryResolveUv(RaycastHit hit, out Vector2 uv)
        {
            switch (projection)
            {
                case ScratchProjection.DecalProjector:
                    return TryWorldToUv(hit.point, out uv);
                case ScratchProjection.SurfaceUv:
                    if (surfaceCollider != null && hit.collider != surfaceCollider)
                    {
                        uv = default;
                        return false;
                    }
                    uv = hit.textureCoord;
                    return true;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(ScratchProjection)}: {projection}");
                    uv = default;
                    return false;
            }
        }

        /// <summary> Мировая точка → UV проекции декали (центр-маппинг по size/pivot, с калибровкой осей) </summary>
        private bool TryWorldToUv(Vector3 worldPoint, out Vector2 uv)
        {
            uv = default;

            Vector3 size = dustDecal.size;
            if (Mathf.Approximately(size.x, 0f) || Mathf.Approximately(size.y, 0f)) return false;

            Vector3 local = dustDecal.transform.InverseTransformPoint(worldPoint) - dustDecal.pivot;
            float u = local.x / size.x + 0.5f;
            float v = local.y / size.y + 0.5f;

            if (swapUV) (u, v) = (v, u);
            if (flipU) u = 1f - u;
            if (flipV) v = 1f - v;

            if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

            uv = new Vector2(u, v);
            return true;
        }

        #endregion

        #region Фидбэк

        private void OnStepCompletionChanged(bool completed)
        {
            // Шаг откатился в «не завершён» — возвращаем слой (re-fog)
            if (!completed) SetErased(false);
        }

        /// <summary> Подвести эмиттер и звук к точке контакта (запустить при первом касании) </summary>
        private void UpdateFeedback(Vector3 point, Vector3 normal)
        {
            if (dustParticles != null)
            {
                dustParticles.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));

                if (!feedbackActive)
                {
                    dustParticles.Clear();
                    dustParticles.Play();
                }
            }

            if (!feedbackActive)
            {
                if (scratchSound != null && AudioController.Instance != null)
                    loopSource = AudioController.Instance.Play(scratchSound, point, AudioModel.Sfx, loop: true);

                feedbackActive = true;
            }

            if (loopSource != null) loopSource.transform.position = point;
        }

        private void StopFeedback()
        {
            if (!feedbackActive) return;
            feedbackActive = false;

            if (dustParticles != null)
                dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (loopSource != null)
            {
                if (AudioController.Instance != null) AudioController.Instance.Stop(loopSource);
                loopSource = null;
            }
        }

        #endregion

        #region Сейв

        private void LoadSavedMask()
        {
            if (!saveProgress || SaveKey == null) return;
            if (!JsonSaveLoad.Exists(SaveKey)) return;

            ScratchSave data = JsonSaveLoad.Load<ScratchSave>(SaveKey);
            if (data.mask == null || data.mask.Length == 0) return;
            if (!mask.LoadImage(data.mask)) return;

            mask.wrapMode = TextureWrapMode.Clamp;
            mask.filterMode = FilterMode.Bilinear;

            pixels = mask.GetPixels32();
            totalTexels = pixels.Length;
            RecountCleared();
        }

        private void SaveMask()
        {
            if (!saveProgress || SaveKey == null || !saveDirty || mask == null) return;

            // Персистим маску только у решённого шага — симметрично загрузке: незачтённое частичное стирание сбрасывается
            if (step != null && !step.IsCompleted) return;

            // Перед кодированием убеждаемся, что последние мазки уже в текстуре
            if (dirty)
            {
                mask.SetPixels32(pixels);
                mask.Apply(false);
                dirty = false;
            }

            JsonSaveLoad.Save(new ScratchSave { mask = mask.EncodeToPNG() }, SaveKey);
            saveDirty = false;
        }

        #endregion

        #region Internal

        private Material ResolveMaterialSource()
        {
            switch (projection)
            {
                case ScratchProjection.DecalProjector:
                    if (dustDecal == null)
                    {
                        ServiceDebug.LogError(this, $"Режим {projection}: не назначен {nameof(dustDecal)}");
                        return null;
                    }
                    if (dustDecal.material == null)
                    {
                        ServiceDebug.LogError(this, "У декали нет материала — маску не назначить");
                        return null;
                    }
                    return dustDecal.material;
                case ScratchProjection.SurfaceUv:
                    if (dustRenderer == null)
                    {
                        ServiceDebug.LogError(this, $"Режим {projection}: не назначен {nameof(dustRenderer)}");
                        return null;
                    }
                    if (dustRenderer.sharedMaterial == null)
                    {
                        ServiceDebug.LogError(this, "У меша нет материала — маску не назначить");
                        return null;
                    }
                    return dustRenderer.sharedMaterial;
                default:
                    ServiceDebug.LogError(this, $"Необработанный {nameof(ScratchProjection)}: {projection}");
                    return null;
            }
        }

        private void AssignMaterial(Material instance)
        {
            switch (projection)
            {
                case ScratchProjection.DecalProjector: dustDecal.material = instance; break;
                case ScratchProjection.SurfaceUv: dustRenderer.material = instance; break;
                default: ServiceDebug.LogError(this, $"Необработанный {nameof(ScratchProjection)}: {projection}"); break;
            }
        }

        private void BuildMask()
        {
            maskResolution = Mathf.Max(MIN_RESOLUTION, maskResolution);
            totalTexels = maskResolution * maskResolution;

            mask = new Texture2D(maskResolution, maskResolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            pixels = new Color32[totalTexels];
            Color32 present = dustPresent;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = present;

            clearedTexels = 0;
            mask.SetPixels32(pixels);
            mask.Apply(false);
        }

        private void RecountCleared()
        {
            int cleared = 0;
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].r < PRESENT_THRESHOLD) cleared++;

            clearedTexels = cleared;
        }

        #endregion
    }
}
