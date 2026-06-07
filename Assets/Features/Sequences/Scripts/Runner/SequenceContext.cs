using Extensions.Events;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Контекст последовательности (компонент на корне префаба): владеет хабом событий загадки
    /// </summary>
    /// <remarks>
    /// Одна загадка = один префаб = один контекст; события не текут между загадками
    /// </remarks>
    public sealed class SequenceContext : MonoBehaviour
    {
        /// <summary> Хаб событий последовательности </summary>
        public EventHub Hub => hub ??= new EventHub();

        private EventHub hub;

        private void OnDestroy() => hub?.Clear();
    }
}
