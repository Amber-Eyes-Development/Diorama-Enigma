using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Зона приземления для перетаскиваемых объектов
    /// </summary>
    public sealed class DropZoneObject : MonoBehaviour
    {
        /// <summary> ID зоны </summary>
        public string ZoneId => zoneId != null ? zoneId.Id : string.Empty;

        [SerializeField] private InteractableID zoneId;

        /// <summary> Принять перетащенный объект </summary>
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
