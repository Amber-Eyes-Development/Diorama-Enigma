using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Идентификатор интерактивного объекта <see cref="InteractableObject"/> или зоны <see cref="DropZoneObject"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/InteractableID", fileName = nameof(InteractableID))]
    public sealed class InteractableID : ID { }
}
