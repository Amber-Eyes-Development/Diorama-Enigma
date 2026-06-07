using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие над системой частиц
    /// </summary>
    [Serializable]
    public struct ParticleSystemAction
    {
        [Tooltip("Целевая система частиц")]
        public ParticleSystem Particles;
        [Tooltip("Что сделать с системой при срабатывании")]
        public ParticleSystemCommand Command;

        /// <summary> Выполнить действие </summary>
        /// <param name="silent">
        /// Тихий режим: привести систему в установившееся состояние без видимого всплеска спавна
        /// (восстановление после загрузки). Влияет только на <see cref="ParticleSystemCommand.Play"/>
        /// </param>
        public readonly void Apply(bool silent)
        {
            if (Particles == null) return;

            switch (Command)
            {
                case ParticleSystemCommand.Play:
                    Play(silent);
                    break;

                case ParticleSystemCommand.Stop:
                    Particles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                    break;
            }
        }

        private readonly void Play(bool silent)
        {
            if (silent)
            {
                Particles.Simulate(Particles.main.duration, withChildren: true, restart: true);
                if (Particles.main.loop) Particles.Play(withChildren: true);
                return;
            }

            Particles.Play(withChildren: true);
        }
    }
}
