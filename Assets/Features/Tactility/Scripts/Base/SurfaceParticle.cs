using System;
using UnityEngine;

namespace DioramaEnigma.Tactility
{
    /// <summary> Пара «поверхность → партикл-эффект» </summary>
    [Serializable]
    public struct SurfaceParticle
    {
        public PhysicsMaterial Material;
        public ParticleSystem Particle;
    }
}