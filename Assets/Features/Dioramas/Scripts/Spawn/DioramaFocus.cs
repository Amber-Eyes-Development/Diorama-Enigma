using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Данные смены фокуса диорамы
    /// </summary>
    public readonly struct DioramaFocus
    {
        /// <summary> Мировая точка кадрирования активной диорамы </summary>
        public Vector3 Point { get; }
        /// <summary> Анимировать переход (навигация) или встать мгновенно (старт/переукладка очереди) </summary>
        public bool Animate { get; }

        public DioramaFocus(Vector3 point, bool animate)
        {
            Point = point;
            Animate = animate;
        }
    }
}
