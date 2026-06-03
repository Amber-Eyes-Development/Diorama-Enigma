using Extensions.Events;
using Extensions.Singleton;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Сцено-уровневый контекст системы загадок
    /// </summary>
    public sealed class RiddleContext : MonoBehaviourSingleton<RiddleContext>
    {
        /// <summary>Хаб событий</summary>
        public EventHub Hub => hub ??= new EventHub();

        private EventHub hub;

        private void OnDestroy() => hub?.Clear();
    }
}
