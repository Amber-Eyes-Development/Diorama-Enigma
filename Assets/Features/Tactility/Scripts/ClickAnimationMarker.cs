using DG.Tweening;
using UnityEngine;

namespace DioramaEnigma.Tactility
{
    /// <summary>
    /// Маркер анимации <see cref="DOTweenAnimation"/> при клике на объект (из <see cref="ClickAnimationPool"/>).
    /// </summary>
    public sealed class ClickAnimationMarker : MonoBehaviour
    {
        [Tooltip("Референсный DOTweenAnimation-шаблон (набор параметров анимации клика)")]
        [SerializeField] private DOTweenAnimation template;
        [Tooltip("Цель анимации. Если не задана — используется собственный Transform")]
        [SerializeField] private Transform animationTarget;

        /// <summary> Референсный шаблон анимации </summary>
        public DOTweenAnimation Template => template;
        /// <summary> Объект, к которому применяется анимация </summary>
        public Transform Target => animationTarget != null ? animationTarget : transform;
    }
}
