using Extensions.RuntimeReferences;
using LeTai.Asset.TranslucentImage;
using UnityEngine;

namespace DioramaEnigma.CameraUtils
{
    /// <summary>
    /// Инициализатор источника размытия для элемента UI
    /// </summary>
    [RequireComponent(typeof(TranslucentImage))]
    public class TranslucentSourceInitializer : RuntimeReferenceConsumer<TranslucentImageSource>
    {
        private TranslucentImage translucentImage;

        protected void Awake()
        {
            translucentImage = GetComponent<TranslucentImage>();
        }

        protected override void OnInitialized(TranslucentImageSource value)
        {
            translucentImage.source = value;
        }
    }
}