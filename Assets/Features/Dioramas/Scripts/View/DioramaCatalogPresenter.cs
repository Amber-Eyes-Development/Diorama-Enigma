using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("selection")]
        [SerializeField] private DioramaBlockSelection blockSelection;
        [Tooltip("Выбранная диорама (ассет-мост в игровую сцену)")]
        [SerializeField] private DioramaSelection dioramaSelection;

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

        [Header("Заглушки")]
        [Tooltip("Префаб заглушки блока — пустышка-намёк, что впереди есть ещё скрытые блоки")]
        [SerializeField] private GameObject blockStubPrefab;
        [Tooltip("Префаб заглушки диорамы — пустышка-намёк, что в блоке есть ещё закрытые диорамы")]
        [SerializeField] private GameObject dioramaStubPrefab;
        [Tooltip("Показывать только одну заглушку, сколько бы блоков/диорам ни было скрыто")]
        [SerializeField] private bool onlyOneStub;

        private readonly List<GameObject> blockButtons = new();
        private readonly List<GameObject> dioramaButtons = new();
        private readonly List<GameObject> blockStubs = new();
        private readonly List<GameObject> dioramaStubs = new();

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
            access.onBlockRestarted += OnBlockUnlocked;
            access.onProgressReset += Rebuild;

            Rebuild();
        }

        private void OnDisable()
        {
            if (access != null)
            {
                access.onDioramaUnlocked -= OnGraphChanged;
                access.onDioramaCompleted -= OnGraphChanged;
                access.onBlockUnlocked -= OnBlockUnlocked;
                access.onBlockRestarted -= OnBlockUnlocked;
                access.onProgressReset -= Rebuild;
            }

            ClearButtons(blockButtons);
            ClearButtons(dioramaButtons);
            ClearButtons(blockStubs);
            ClearButtons(dioramaStubs);
        }

        public void Rebuild()
        {
            GameObjectUtils.ClearChildren(blockContainer);
            GameObjectUtils.ClearChildren(dioramaContainer);

            BuildBlocks();
            SyncSelection();
            BuildDioramas();
        }

        private void OnGraphChanged(DioramaDefinition _) => Rebuild();

        private void OnBlockUnlocked(DioramaBlock _) => Rebuild();

        private void BuildBlocks()
        {
            ClearButtons(blockButtons);
            ClearButtons(blockStubs);
            if (access == null) return;

            var blocks = access.VisibleBlocks();

            if (selectedBlock == null && blockSelection != null)
                selectedBlock = access.BlockById(blockSelection.SelectedId);

            bool selectedStillVisible = false;

            foreach (var block in blocks)
            {
                var view = Instantiate(blockButtonPrefab, blockContainer);
                view.Bind(block, access.CompletedCountInBlock(block), access.AllInBlock(block).Count, OnBlockSelected);
                blockButtons.Add(view.gameObject);

                if (block == selectedBlock) selectedStillVisible = true;
            }

            if (!selectedStillVisible)
                selectedBlock = blocks.Count > 0 ? blocks[0] : null;

            SpawnStubs(blockContainer, blockStubPrefab, blockStubs, access.HiddenBlockCount());
        }

        private void OnBlockSelected(DioramaBlock block)
        {
            selectedBlock = block;
            SyncSelection();
            BuildDioramas();
        }

        // Привести контексты выбора к активному блоку: блок — в контекст блока, диораму — в контекст диорамы
        private void SyncSelection()
        {
            if (selectedBlock == null) return;
            if (blockSelection != null) blockSelection.Select(selectedBlock.Id);
            SelectDioramaForBlock();
        }

        // Текущий выбор диорамы оставляем, если он принадлежит блоку; иначе берём последнюю открытую в блоке
        private void SelectDioramaForBlock()
        {
            if (dioramaSelection == null || access == null) return;

            var dioramas = access.DioramasInBlock(selectedBlock);
            if (dioramas.Count == 0)
            {
                dioramaSelection.Clear();
                return;
            }

            foreach (var node in dioramas)
                if (node.Definition != null && node.Definition.Id == dioramaSelection.SelectedId) return;

            dioramaSelection.Select(dioramas[dioramas.Count - 1].Definition.Id);
        }

        private void BuildDioramas()
        {
            ClearButtons(dioramaButtons);
            ClearButtons(dioramaStubs);
            if (access == null || selectedBlock == null) return;

            foreach (var node in access.DioramasInBlock(selectedBlock))
            {
                var view = Instantiate(dioramaButtonPrefab, dioramaContainer);
                view.Bind(node.Definition, node.State, node.FullyCompleted, OnDioramaSelected);
                dioramaButtons.Add(view.gameObject);
            }

            SpawnStubs(dioramaContainer, dioramaStubPrefab, dioramaStubs, access.HiddenDioramaCount(selectedBlock));
        }

        // Дозаполнить контейнер пустышками после реальных кнопок: по числу скрытых элементов либо одной (onlyOneStub)
        private void SpawnStubs(Transform container, GameObject stubPrefab, List<GameObject> stubs, int hiddenCount)
        {
            if (stubPrefab == null || hiddenCount <= 0) return;

            int count = onlyOneStub ? 1 : hiddenCount;
            for (int i = 0; i < count; i++)
                stubs.Add(Instantiate(stubPrefab, container));
        }

        private void OnDioramaSelected(DioramaDefinition def)
        {
            if (selectedBlock != null && blockSelection != null) blockSelection.Select(selectedBlock.Id);
            if (def != null && dioramaSelection != null) dioramaSelection.Select(def.Id);
        }

        private void ClearButtons(List<GameObject> buttons)
        {
            foreach (var go in buttons)
                if (go != null) Destroy(go);

            buttons.Clear();
        }
    }
}
