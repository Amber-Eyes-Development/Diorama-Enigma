using System;
using Extensions.Generics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка диорамы в карте: иконка, название и индикация состояния; по клику — переход к диораме
    /// </summary>
    /// <remarks> Пройденная диорама подсвечивается приглушённым цветом (tintTarget) </remarks>
    public sealed class DioramaButtonView : AbstractButtonAction
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        [Header("Состояние «пройдено»")]
        [Tooltip("Графика, цвет которой меняется для пройденной диорамы (фон/рамка)")]
        [SerializeField] private Graphic tintTarget;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color completedColor = new(0.6f, 0.6f, 0.6f, 1f);

        private DioramaDefinition definition;
        private Action<DioramaDefinition> onSelected;

        /// <summary> Привязать кнопку к диораме и её состоянию </summary>
        /// <param name="definition">Определение диорамы</param>
        /// <param name="state">Состояние доступа</param>
        /// <param name="onSelected">Колбэк выбора диорамы</param>
        public void Bind(DioramaDefinition definition, DioramaState state, Action<DioramaDefinition> onSelected)
        {
            this.definition = definition;
            this.onSelected = onSelected;

            if (label != null) label.text = definition != null ? definition.Title : string.Empty;
            if (icon != null) icon.sprite = definition != null ? definition.Icon : null;
            if (tintTarget != null) tintTarget.color = state == DioramaState.Completed ? completedColor : normalColor;
        }

        public override void OnButtonClickAction() => onSelected?.Invoke(definition);
    }
}
