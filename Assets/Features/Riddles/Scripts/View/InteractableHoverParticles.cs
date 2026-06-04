using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Реакция системой частиц на наведение и уход курсора
    /// </summary>
    public sealed class InteractableHoverParticles : InteractableViewBehaviour
    {
        [Header("При наведении"), Space]
        [SerializeField] private ParticleSystemAction[] onEnter;

        [Header("При уходе"), Space]
        [SerializeField] private ParticleSystemAction[] onExit;

        protected override void Subscribe() => Interactable.onHoverChanged += OnHoverChanged;

        protected override void Unsubscribe() => Interactable.onHoverChanged -= OnHoverChanged;

        private void OnHoverChanged(bool isHovered)
        {
            var actions = isHovered ? onEnter : onExit;

            actions.Apply(silent: false);
        }
    }
}
