using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Tactility
{
    /// <summary> Пара «поверхность → звук» </summary>
    [Serializable]
    public struct SurfaceSoundPair
    {
        public PhysicsMaterial Material;
        public AudioResource Sound;
    }
}
