using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Каталог диорам в главном меню (спавнер кнопок выбора локации/диорамы)
    /// </summary>
    public sealed class DioramaCatalogPresenter : MonoBehaviour
    {
        [Header("Источник")]
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Выбранный блок (ассет-мост в игровую сцену)")]
        [SerializeField] private DioramaBlockSelection selection;

        [Header("Блоки")]
        [Tooltip("Контейнер кнопок блоков")]
        [SerializeField] private Transform blockContainer;
        [Tooltip("Префаб кнопки блока")]
        [SerializeField] private DioramaBlockButtonView blockButtonPrefab;

        [Header("Диорамы")]
        [Tooltip("Контейнер кнопок диорам выбранного блока")]
        [SerializeField] private Transform dioramaContainer;
        [Tooltip("Префаб кнопки диорамы")]
        [SerializeField] private DioramaButtonView dioramaButtonPrefab;

        private readonly List<GameObject> blockButtons = new();
        private readonly List<GameObject> dioramaButtons = new();

        private DioramaBlock selectedBlock;

        private void OnEnable()
        {
            if (access == null)
            {
                ServiceDebug.LogError($"{nameof(access)} не назначен");
                return;
            }

            access.onDioramaUnlocked += OnGraphChanged;
            access.onDioramaCompleted += OnGraphChanged;
            access.onBlockUnlocked += OnBlockUnlocked;

            Rebuild();
        }

        private void OnDisable()
        {
            if (access != null)
            {
                access.onDioramaUnlocked -= OnGraphChanged;
                access.onDioramaCompleted -= OnGraphChanged;
                access.onBlockUnlocked -= OnBlockUnlocked;
            }

            ClearButtons(blockButtons);
            ClearButtons(dioramaButtons);
        }

        public void Rebuild()
        {
            GameObjectUtils.ClearChildren(blockContainer);
            GameObjectUtils.ClearChildren(dioramaContainer);
            
            BuildBlocks();
            BuildDioramas();
        }

        private void OnGraphChanged(DioramaDefinition _) => Rebuild();

        private void OnBlockUnlocked(DioramaBlock _) => Rebuild();

        private void BuildBlocks()
        {
            ClearButtons(blockButtons);
            if (access == null) return;

            var blocks = access.VisibleBlocks();
            bool selectedStillVisible = false;

            foreach (var block in blocks)
            {
                var view = Instantiate(blockButtonPrefab, blockContainer);
                view.Bind(block, OnBlockSelected);
                blockButtons.Add(view.gameObject);

                if (block == selectedBlock) selectedStillVisible = true;
            }

            if (!selectedStillVisible)
                selectedBlock = blocks.Count > 0 ? blocks[0] : null;
        }

        private void OnBlockSelected(DioramaBlock block)
        {
            selectedBlock = block;
            if (selectedBlock != null && selection != null) selection.Select(selectedBlock.Id);
            BuildDioramas();
        }

        private void BuildDioramas()
        {
            ClearButtons(dioramaButtons);
            if (access == null || selectedBlock == null) return;

            foreach (var node in access.DioramasInBlock(selectedBlock))
            {
                var view = Instantiate(dioramaButtonPrefab, dioramaContainer);
                view.Bind(node.Definition, node.State, OnDioramaSelected);
                dioramaButtons.Add(view.gameObject);
            }
        }

        private void OnDioramaSelected(DioramaDefinition def)
        {
            if (selectedBlock != null && selection != null) selection.Select(selectedBlock.Id);
            DioramaProgressStore.SaveLastActive(def.Id);
        }

        private void ClearButtons(List<GameObject> buttons)
        {
            foreach (var go in buttons)
                if (go != null) Destroy(go);

            buttons.Clear();
        }
    }
}
