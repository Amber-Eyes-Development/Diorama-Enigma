using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Зона приземления для дрэг-объектов. Публикует событие перетаскивания через <see cref="InteractableObject"/>.
    /// </summary>
    public sealed class DropZoneObject : MonoBehaviour
    {
        /// <summary>ID зоны (используется как DropZoneId в событиях)</summary>
        public string ZoneId => zoneId != null ? zoneId.Id : string.Empty;

        [SerializeField] private InteractableID zoneId;

        /// <summary>Принять перетащенный объект и уведомить его о факте дропа на эту зону</summary>
        public void ReceiveDrop(InteractableObject dragged)
        {
            if (dragged == null)
            {
                ServiceDebug.LogWarning(this, "ReceiveDrop вызван с null объектом");
                return;
            }

            dragged.OnDroppedOnZone(this);
        }
    }
}
