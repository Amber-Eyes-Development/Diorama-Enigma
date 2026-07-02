using System;
using Extensions.Generics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка блока в карте: иконка и название; по клику сообщает выбранный блок презентеру
    /// </summary>
    public sealed class DioramaBlockButtonView : AbstractButtonAction
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [Tooltip("Индикация активного выбора блока (опционально)")]
        [SerializeField] private DioramaSelectionIndicator selectionIndicator;

        [Header("Прогресс")]
        [Tooltip("Текст прогресса блока «пройдено/всего» (опционально)")]
        [SerializeField] private TMP_Text progressLabel;
        [Tooltip("Галочка полностью пройденного блока (опционально)")]
        [SerializeField] private GameObject completedCheckmark;

        private DioramaBlock block;
        private Action<DioramaBlock> onSelected;

        /// <summary> Привязать кнопку к блоку и его прогрессу </summary>
        /// <param name="block">Блок</param>
        /// <param name="completed">Сколько диорам блока пройдено</param>
        /// <param name="total">Сколько всего диорам в блоке</param>
        /// <param name="onSelected">Колбэк выбора блока</param>
        public void Bind(DioramaBlock block, int completed, int total, Action<DioramaBlock> onSelected)
        {
            this.block = block;
            this.onSelected = onSelected;

            if (label != null) label.text = block != null ? block.Title : string.Empty;
            if (icon != null) icon.sprite = block != null ? block.Icon : null;
            if (selectionIndicator != null) selectionIndicator.Bind(block != null ? block.Id : null);

            if (progressLabel != null) progressLabel.text = total > 0 ? $"{completed}/{total}" : string.Empty;
            if (completedCheckmark != null) completedCheckmark.SetActive(total > 0 && completed >= total);

            // Дать расширениям кнопки (напр. рекорду из статистики) узнать блок — без обратной зависимости на их фичи
            foreach (var listener in GetComponents<IDioramaButtonBindListener>())
                listener.OnBound(block);
        }

        public override void OnButtonClickAction() => onSelected?.Invoke(block);
    }
}
