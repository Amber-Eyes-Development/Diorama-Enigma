using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Inventory
{
    /// <summary> Канал рантайм-ссылки на панель инвентаря </summary>
    [CreateAssetMenu(menuName = "Inventory/View Reference", fileName = nameof(InventoryViewReference))]
    public sealed class InventoryViewReference : RuntimeReference<IInventoryView> { }
}
