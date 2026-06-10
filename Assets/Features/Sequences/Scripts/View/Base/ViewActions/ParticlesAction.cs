using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: команда системе частиц (откат = противоположная команда)
    /// </summary>
    [Serializable]
    public sealed class ParticlesAction : ViewAction
    {
        /// <summary> Целевая система частиц </summary>
        public ParticleSystem Target => target;
        /// <summary> Команда при срабатывании триггера </summary>
        public ParticleSystemCommand Command => command;

        public override void EnsureTarget(GameObject host)
        {
            if (target == null)
                target = host.GetComponent<ParticleSystem>() ?? host.AddComponent<ParticleSystem>();
        }

        [Tooltip("Целевая система частиц")]
        [SerializeField] private ParticleSystem target;
        [Tooltip("Команда при срабатывании триггера")]
        [SerializeField] private ParticleSystemCommand command;
    }
}
