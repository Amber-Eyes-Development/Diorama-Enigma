using Extensions.Log;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DioramaEnigma.Scratch
{
    /// <summary>
    /// Стираемый слой: владеет маской-текстурой декали и стирает её кистью
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScratchSurface : MonoBehaviour
    {
        private const string MASK_PROPERTY = "_ScratchMask";
        private const int MIN_RESOLUTION = 8;
        private const byte PRESENT_THRESHOLD = 128;

        /// <summary> Доля декали в диапазоне 0..1 </summary>
        public float Progress => totalTexels == 0 ? 0f : clearedTexels / (float)totalTexels;

        [Tooltip("Декаль-проектор: его материал будет клонирован, в инстанс назначится маска _ScratchMask")]
        [SerializeField] private DecalProjector dustDecal;
        [Tooltip("Разрешение маски стирания (квадрат)")]
        [Min(MIN_RESOLUTION)]
        [SerializeField] private int maskResolution = 256;
        [Tooltip("Цвет «декаль на месте» (для шейдера важен только R-канал)")]
        [SerializeField] private Color dustPresent = Color.white;

        [Header("Калибровка маппинга курсор-маска"), Space]
        [Tooltip("Зеркалить ось U")]
        [SerializeField] private bool flipU;
        [Tooltip("Зеркалить ось V")]
        [SerializeField] private bool flipV;
        [Tooltip("Поменять U и V местами")]
        [SerializeField] private bool swapUV;

        private static readonly Color32 Erased = new(0, 0, 0, 0);

        private Texture2D mask;
        private Material materialInstance;
        private Color32[] pixels;
        private int clearedTexels;
        private int totalTexels;
        private bool dirty;

        private void Awake()
        {
            if (dustDecal == null)
            {
                ServiceDebug.LogError(this, $"Не назначен {nameof(dustDecal)} — стирать нечего");
                enabled = false;
                return;
            }

            Material source = dustDecal.material;
            if (source == null)
            {
                ServiceDebug.LogError(this, "У декали нет материала — маску не назначить");
                enabled = false;
                return;
            }

            BuildMask();

            materialInstance = new Material(source);
            materialInstance.SetTexture(MASK_PROPERTY, mask);
            dustDecal.material = materialInstance;
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

        /// <summary> Стереть декаль кистью в мировой точке (если она попадает в проекцию декали) </summary>
        /// <param name="worldPoint">Точка контакта на поверхности</param>
        /// <param name="radiusUv">Радиус кисти в долях UV (0..1)</param>
        /// <returns>true — что-то было стёрто этим мазком</returns>
        public bool TryEraseAt(Vector3 worldPoint, float radiusUv)
        {
            if (pixels == null) return false;
            if (!TryWorldToUv(worldPoint, out float u, out float v)) return false;

            int centerX = Mathf.RoundToInt(u * (maskResolution - 1));
            int centerY = Mathf.RoundToInt(v * (maskResolution - 1));
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

            if (changed) dirty = true;
            return changed;
        }

        /// <summary> Полностью стереть декаль (завершение) либо вернуть её целиком (сброс) </summary>
        /// <param name="full">true — стена чистая; false — пыль на месте</param>
        public void SetErased(bool full)
        {
            if (pixels == null) return;

            Color32 fill = full ? Erased : (Color32)dustPresent;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;

            clearedTexels = full ? totalTexels : 0;
            dirty = true;
        }

        #region Internal

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

        /// <summary> Мировая точка → UV проекции декали (центр-маппинг по size/pivot, с калибровкой осей) </summary>
        private bool TryWorldToUv(Vector3 worldPoint, out float u, out float v)
        {
            u = 0f;
            v = 0f;

            Vector3 size = dustDecal.size;
            if (Mathf.Approximately(size.x, 0f) || Mathf.Approximately(size.y, 0f)) return false;

            Vector3 local = dustDecal.transform.InverseTransformPoint(worldPoint) - dustDecal.pivot;
            u = local.x / size.x + 0.5f;
            v = local.y / size.y + 0.5f;

            if (swapUV) (u, v) = (v, u);
            if (flipU) u = 1f - u;
            if (flipV) v = 1f - v;

            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }

        #endregion
    }
}
