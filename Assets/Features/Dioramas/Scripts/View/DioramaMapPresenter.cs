using System.Collections.Generic;
using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Презентер карты диорам: кнопки открытых блоков; по выбору блока — кнопки его открытых диорам;
    /// по выбору диорамы — переход к ней через спавнер
    /// </summary>
    /// <remarks>
    /// Спавнер берётся из канала <see cref="DioramaSpawnerReference"/> (база <see cref="RuntimeReferenceConsumer{T}"/>),
    /// сервис доступа — из спавнера. Перестраивается на события сервиса (открытие/прохождение диорам, появление блоков)
    /// </remarks>
    public sealed class DioramaMapPresenter : RuntimeReferenceConsumer<DioramaSpawner>
    {
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

        private DioramaAccessService access;
        private DioramaBlock selectedBlock;

        // спавнер появился в канале → подключиться к сервису и построить карту
        protected override void OnInitialized(DioramaSpawner spawner)
        {
            access = spawner.Access;
            if (access == null) return;

            access.onDioramaUnlocked += OnGraphChanged;
            access.onDioramaCompleted += OnGraphChanged;
            access.onBlockUnlocked += OnBlockUnlocked;

            Rebuild();
        }

        // спавнер снят или презентер выключается → отписаться и очистить
        protected override void OnReleased()
        {
            if (access != null)
            {
                access.onDioramaUnlocked -= OnGraphChanged;
                access.onDioramaCompleted -= OnGraphChanged;
                access.onBlockUnlocked -= OnBlockUnlocked;
                access = null;
            }

            ClearButtons(blockButtons);
            ClearButtons(dioramaButtons);
        }

        /// <summary> Перестроить кнопки блоков и диорам выбранного блока </summary>
        public void Rebuild()
        {
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
            if (Value != null) Value.FocusDiorama(def);
        }

        private void ClearButtons(List<GameObject> buttons)
        {
            foreach (var go in buttons)
                if (go != null) Destroy(go);

            buttons.Clear();
        }
    }
}
