using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Индикатор-точки: по точке на каждую диораму активного блока
    /// </summary>
    public sealed class DioramaDotsIndicator : RuntimeReferenceConsumer<DioramaSpawner>
    {
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Контейнер точек")]
        [SerializeField] private Transform container;
        [Tooltip("Префаб точки")]
        [SerializeField] private DioramaDotView dotPrefab;

        private readonly List<DioramaDotView> dots = new();
        private DioramaSpawner spawner;

        protected override void OnInitialized(DioramaSpawner value)
        {
            spawner = value;
            spawner.onFocusChanged += OnFocusChanged;
            if (access != null) access.onDioramaUnlocked += OnUnlocked;

            Build();
        }

        protected override void OnReleased()
        {
            if (spawner != null) spawner.onFocusChanged -= OnFocusChanged;
            if (access != null) access.onDioramaUnlocked -= OnUnlocked;

            Clear();
            spawner = null;
        }

        private void OnFocusChanged(DioramaFocus _) => RefreshActive();

        private void OnUnlocked(DioramaDefinition _) => RefreshStates();

        private void Build()
        {
            Clear();
            if (spawner == null || dotPrefab == null || container == null) return;

            foreach (var _ in spawner.BlockDioramas)
                dots.Add(Instantiate(dotPrefab, container));

            RefreshStates();
            RefreshActive();
        }

        private void RefreshStates()
        {
            var defs = spawner.BlockDioramas;
            for (int i = 0; i < dots.Count && i < defs.Count; i++)
                dots[i].SetUnlocked(access != null && access.IsUnlocked(defs[i]));
        }

        private void RefreshActive()
        {
            var defs = spawner.BlockDioramas;
            var active = spawner.Active;
            for (int i = 0; i < dots.Count && i < defs.Count; i++)
                dots[i].SetActiveMarker(defs[i] == active);
        }

        private void Clear()
        {
            GameObjectUtils.ClearChildren(container);
            dots.Clear();
        }
    }
}
