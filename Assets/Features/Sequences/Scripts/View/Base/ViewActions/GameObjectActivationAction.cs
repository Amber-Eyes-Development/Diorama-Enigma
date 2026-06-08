using System;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: переключение активности объекта
    /// </summary>
    [Serializable]
    public sealed class GameObjectActivationAction : ViewAction
    {
        /// <summary> Целевой объект </summary>
        public GameObject Target => target;
        /// <summary> Целевая активность при срабатывании триггера (при откате — противоположная) </summary>
        public bool Active => active;

        [Tooltip("Целевой объект")]
        [SerializeField] private GameObject target;
        [Tooltip("Включить (true) или выключить (false) при срабатывании триггера")]
        [SerializeField] private bool active;
    }
}
