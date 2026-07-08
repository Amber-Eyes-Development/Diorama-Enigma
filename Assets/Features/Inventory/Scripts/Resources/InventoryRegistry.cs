using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Inventory
{
    /// <summary> Реестр предметов инвентаря — список всех <see cref="ResourceValue"/> для UI и лукапа </summary>
    [CreateAssetMenu(menuName = "Inventory/Registry", fileName = nameof(InventoryRegistry))]
    public sealed class InventoryRegistry : IdentifiableRegistry<ResourceValue> { }
}
