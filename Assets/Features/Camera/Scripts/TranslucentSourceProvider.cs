using Extensions.RuntimeReferences;
using LeTai.Asset.TranslucentImage;
using UnityEngine;

namespace DioramaEnigma.CameraUtils
{
    /// <summary>
    /// Провайдер источника размытия <see cref="TranslucentImageSource"/>
    /// </summary>
    [RequireComponent(typeof(TranslucentImageSource))]
    public class TranslucentSourceProvider : RuntimeReferenceProvider<TranslucentImageSource> { }
}