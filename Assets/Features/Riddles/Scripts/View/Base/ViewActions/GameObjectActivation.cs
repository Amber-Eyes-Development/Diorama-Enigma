using System;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Переключение активности объекта
    /// </summary>
    [Serializable]
    public struct GameObjectActivation
    {
        [Tooltip("Целевой объект")]
        public GameObject Target;

        [Tooltip("Включить (true) или выключить (false) объект")]
        public bool Active;

        /// <summary> Применить активность к объекту </summary>
        public readonly void Apply()
        {
            if (Target != null) Target.SetActive(Active);
        }
    }
}
