#if VHIERARCHY
using UnityEngine;
using VHierarchy;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Правило VHierarchy: помечает в иерархии сцены объекты с <see cref="InteractableInput"/> иконкой-кружком
    /// </summary>
    internal static class InteractableHierarchyIcon
    {
        // Встроенная иконка-гизмо Unity (16px, заполненный кружок)
        private const string CircleIcon = "sv_icon_dot0_pix16_gizmo";

        [Rule]
        private static void Apply(ObjectInfo info)
        {
            if (info.gameObject.GetComponent<InteractableInput>())
                info.icon = CircleIcon;
        }
    }
}
#endif
