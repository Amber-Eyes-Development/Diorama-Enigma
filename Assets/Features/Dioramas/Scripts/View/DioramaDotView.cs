using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Одна точка индикатора
    /// </summary>
    public sealed class DioramaDotView : MonoBehaviour
    {
        [Tooltip("Графика точки (цвет меняется по состоянию)")]
        [SerializeField] private Image dot;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new(0.4f, 0.4f, 0.4f, 1f);
        [Tooltip("Обводка-скоба активной точки")]
        [SerializeField] private GameObject activeMarker;

        /// <summary> Вид точки по доступности диорамы </summary>
        public void SetUnlocked(bool unlocked)
        {
            if (dot != null) dot.color = unlocked ? unlockedColor : lockedColor;
        }

        /// <summary> Показать/скрыть «скобу» активной диорамы </summary>
        public void SetActiveMarker(bool active)
        {
            if (activeMarker != null) activeMarker.SetActive(active);
        }
    }
}
