using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Реакция системами частиц на событие шага
    /// </summary>
    public sealed class InteractableParticles : InteractableReactionBehaviour
    {
        [SerializeField] private ParticleSystemAction[] actions;

        protected override void React(bool silent) => actions.Apply(silent);
    }
}
