using Extensions.Events;
using Extensions.Log;
using Extensions.Singleton;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Сцено-уровневый контекст системы загадок. Размещается один раз в сцене.
    /// Хранит <see cref="EventHub"/> и предоставляет его всем компонентам системы через <see cref="Instance"/>.
    /// </summary>
    public sealed class RiddleContext : MonoBehaviourSingleton<RiddleContext>
    {
        /// <summary>Хаб событий системы загадок</summary>
        public EventHub Hub { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Hub = new EventHub();
        }

        private void OnDestroy() => Hub?.Clear();
    }
}
