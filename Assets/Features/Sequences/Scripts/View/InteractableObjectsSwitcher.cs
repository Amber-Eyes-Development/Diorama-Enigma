using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Реакция на событие шага переключением активности объектов
    /// </summary>
    public sealed class InteractableObjectsSwitcher : InteractableReactionBehaviour
    {
        [SerializeField] private GameObjectActivation[] objects;

        protected override void React(bool silent) => objects.Apply();
    }
}
