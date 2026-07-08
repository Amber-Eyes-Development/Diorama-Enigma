using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Inventory
{
    /// <summary> Канал рантайм-ссылки на всплывающую плашку с текстом предмета </summary>
    [CreateAssetMenu(menuName = "Inventory/Tooltip View Reference", fileName = nameof(TooltipViewReference))]
    public sealed class TooltipViewReference : RuntimeReference<ITooltipView> { }
}
