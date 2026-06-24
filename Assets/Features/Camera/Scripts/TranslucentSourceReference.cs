using Extensions.RuntimeReferences;
using LeTai.Asset.TranslucentImage;
using UnityEngine;

namespace DioramaEnigma.CameraUtils
{
    /// <summary>
    /// Канал рантайм-ссылки на источник размытия картинки камеры
    /// </summary>
    [CreateAssetMenu(menuName = "Camera/Translucent Source Reference", fileName = nameof(TranslucentSourceReference))]
    public class TranslucentSourceReference : RuntimeReference<TranslucentImageSource> { }
}