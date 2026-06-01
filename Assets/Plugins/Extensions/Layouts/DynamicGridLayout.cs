using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Layouts
{
    /// <summary>
    /// Автоматическая подстройка <see cref="GridLayoutGroup"/> под свободное пространство
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class DynamicGridLayout : MonoBehaviour
    {
        [Header("Параметры сетки"), Space]
        [Tooltip("Количество колонн"), Min(1f)]
        [SerializeField] private int columns = 7;
        [Tooltip("Количество рядов"), Min(1f)]
        [SerializeField] private int rows = 6;
        
        [Header("Размеры ячейки"), Space]
        [Tooltip("Расстояние между ячейками"), Min(0f)]
        [SerializeField] private float cellPadding = 5f;
        // [Tooltip("Минимальный размер каждой отдельной ячейки")]
        // [SerializeField] private  float minCellSize = 50f;
        
        [Header("Обновление макета"), Space]
        [Tooltip("Автообновление макета каждый кадр")]
        [SerializeField] private bool updateDynamically = false;

        private RectTransform rectTransform;
        private GridLayoutGroup gridLayout;

        private void Update()
        {
            if (updateDynamically) UpdateGridLayout();
        }
        
        private void OnValidate() => UpdateGridLayout();
        
        private void OnRectTransformDimensionsChange() => UpdateGridLayout();
        
        /// <summary>
        /// Автоматическая подстройка макета под свободное пространство
        /// </summary>
        public void UpdateGridLayout()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (gridLayout == null) gridLayout = GetComponent<GridLayoutGroup>();
            
            float availableWidth = rectTransform.rect.width;
            float availableHeight = rectTransform.rect.height;

            float cellSizeX = (availableWidth - (columns - 1) * cellPadding) / columns;
            float cellSizeY = (availableHeight - (rows - 1) * cellPadding) / rows;

            gridLayout.cellSize = new Vector2(cellSizeX, cellSizeY);
            gridLayout.spacing = new Vector2(cellPadding, cellPadding);
        }
    }
}