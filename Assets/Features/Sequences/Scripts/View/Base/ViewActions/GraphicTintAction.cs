using System;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: окрашивание uGUI-графики на событие шага (напр. дверь карты красным, пока шаг не выполнен)
    /// </summary>
    [Serializable]
    public sealed class GraphicTintAction : ViewAction
    {
        /// <summary> Целевая графика </summary>
        public Graphic Target => target;
        /// <summary> Цвет при срабатывании триггера </summary>
        public Color Color => color;
        /// <summary> Цвет покоя (исходный цвет графики, захваченный при подготовке) </summary>
        public Color RestColor { get; private set; }

        [Tooltip("Целевая графика (если пусто — берётся с этого объекта)")]
        [SerializeField] private Graphic target;
        [Tooltip("Цвет при срабатывании триггера")]
        [SerializeField] private Color color = Color.white;

        /// <inheritdoc/>
        public override void EnsureTarget(GameObject host)
        {
            if (target == null) target = host.GetComponent<Graphic>();
        }

        /// <inheritdoc/>
        public override void PrepareTarget()
        {
            if (target != null) RestColor = target.color;
        }
    }
}
