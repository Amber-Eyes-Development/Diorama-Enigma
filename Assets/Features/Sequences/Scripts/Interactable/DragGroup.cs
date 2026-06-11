using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Категория совместимости перетаскивания (кабели↔розетки, цветы↔горшки)
    /// </summary>
    /// <remarks> Маркер-ассет: объект и зона совместимы по равенству ссылки на одну и ту же группу </remarks>
    [CreateAssetMenu(menuName = "Sequences/Drag Group", fileName = nameof(DragGroup))]
    public sealed class DragGroup : ScriptableObject { }
}
