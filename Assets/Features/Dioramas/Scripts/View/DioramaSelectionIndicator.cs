using Extensions.Data.InMemoryData.SelectionContext;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Индикация активного выбора на кнопке
    /// </summary>
    /// <remarks> Универсальна для блока и диорамы: контекст задаётся ссылкой (<see cref="ISingleSelectionContext"/>) </remarks>
    public sealed class DioramaSelectionIndicator : MonoBehaviour
    {
        [Tooltip("Контекст выбора (блока или диорамы)")]
        [SerializeField] private BaseSelectionContext selection;
        [Tooltip("Графика-подсветка активного выбора (рамка/галочка)")]
        [SerializeField] private GameObject highlight;

        private ISingleSelectionContext single;
        private string id;

        private void Awake()
        {
            single = selection as ISingleSelectionContext;
            if (selection == null || single == null)
                ServiceDebug.LogError($"{nameof(selection)} не назначен или не реализует {nameof(ISingleSelectionContext)}");
        }

        private void OnEnable()
        {
            if (selection != null) selection.onSelectionChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (selection != null) selection.onSelectionChanged -= Refresh;
        }

        /// <summary> Привязать кнопку к её идентификатору (блока/диорамы) </summary>
        /// <param name="id">Id, который представляет эта кнопка</param>
        public void Bind(string id)
        {
            this.id = id;
            Refresh();
        }

        private void Refresh()
        {
            bool active = single != null && !string.IsNullOrEmpty(id) && single.SelectedId == id;
            if (highlight != null) highlight.SetActive(active);
        }
    }
}
