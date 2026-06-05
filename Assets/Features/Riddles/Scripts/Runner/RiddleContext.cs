using Extensions.Events;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Контекст загадки (компонент на корне префаба): владеет хабом событий загадки.
    /// Одна загадка = один префаб = один контекст; события не текут между загадками.
    /// </summary>
    public sealed class RiddleContext : MonoBehaviour
    {
        /// <summary> Хаб событий загадки </summary>
        public EventHub Hub => hub ??= new EventHub();

        private EventHub hub;

        private void OnDestroy() => hub?.Clear();
    }
}
