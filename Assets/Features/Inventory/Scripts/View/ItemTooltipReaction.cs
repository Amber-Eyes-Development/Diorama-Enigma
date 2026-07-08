using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Inventory
{
    /// <summary> Показывает тултип с названием предмета при наведении на слот инвентаря </summary>
    [RequireComponent(typeof(InventoryItemSlotView))]
    public sealed class ItemTooltipReaction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Канал всплывающей плашки")]
        [SerializeField] private TooltipViewReference tooltipReference;

        private InventoryItemSlotView slot;
        private bool isShowing;

        private void Awake() => slot = GetComponent<InventoryItemSlotView>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            var tooltip = tooltipReference != null ? tooltipReference.Current : null;
            if (tooltip == null || slot.Item == null) return;

            isShowing = true;
            tooltip.Show(slot.Item.Title);
        }

        public void OnPointerExit(PointerEventData eventData) => Hide();

        private void OnDisable() => Hide();

        private void Hide()
        {
            if (!isShowing) return;
            isShowing = false;

            var tooltip = tooltipReference != null ? tooltipReference.Current : null;
            tooltip?.Hide();
        }
    }
}
