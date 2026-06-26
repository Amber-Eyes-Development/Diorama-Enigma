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

        private DioramaBlock block;
        private Action<DioramaBlock> onSelected;

        /// <summary> Привязать кнопку к блоку </summary>
        /// <param name="block">Блок</param>
        /// <param name="onSelected">Колбэк выбора блока</param>
        public void Bind(DioramaBlock block, Action<DioramaBlock> onSelected)
        {
            this.block = block;
            this.onSelected = onSelected;

            if (label != null) label.text = block != null ? block.Title : string.Empty;
            if (icon != null) icon.sprite = block != null ? block.Icon : null;
            if (selectionIndicator != null) selectionIndicator.Bind(block != null ? block.Id : null);
        }

        public override void OnButtonClickAction() => onSelected?.Invoke(block);
    }
}
