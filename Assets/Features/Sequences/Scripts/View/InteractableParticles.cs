using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Команды системам частиц на события шага (откат = противоположная команда)
    /// </summary>
    public sealed class InteractableParticles : InteractableActionsBehaviour<ParticlesAction>
    {
        protected override void Apply(ParticlesAction action, bool reverse, bool silent)
        {
            var particles = action.Target;
            if (particles == null) return;

            bool play = action.Command == ParticleSystemCommand.Play;
            if (reverse) play = !play;

            if (play) Play(particles, silent);
            else Stop(particles, silent);
        }

        private static void Play(ParticleSystem particles, bool silent)
        {
            if (silent)
            {
                // Привести к установившемуся состоянию без всплеска спавна
                particles.Simulate(particles.main.duration, withChildren: true, restart: true);
                if (particles.main.loop) particles.Play(withChildren: true);
                return;
            }

            particles.Play(withChildren: true);
        }

        private static void Stop(ParticleSystem particles, bool silent)
        {
            particles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
            if (silent) particles.Clear(withChildren: true);
        }
    }
}
