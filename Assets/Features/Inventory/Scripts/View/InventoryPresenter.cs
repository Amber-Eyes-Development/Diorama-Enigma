using System;
using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using Extensions.Log;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Inventory
{
    /// <summary> Панель инвентаря: слоты предметов, реагирующие на изменение их количества </summary>
    public sealed class InventoryPresenter : MonoBehaviour, IInventoryView
    {
        [Header("Источник")]
        [Tooltip("Реестр предметов (ассет)")]
        [SerializeField] private InventoryRegistry registry;

        [Header("UI")]
        [Tooltip("Контейнер слотов")]
        [SerializeField] private Transform container;
        [Tooltip("Префаб слота предмета")]
        [SerializeField] private InventoryItemSlotView slotPrefab;

        [Header("Что показывать")]
        [Tooltip("Только предметы в наличии (количество > 0)")]
        [SerializeField] private bool showOnlyOwned = true;
        [Tooltip("Только предметы, требуемые текущей (в фокусе) диорамой")]
        [SerializeField] private bool onlyCurrentDiorama;
        [Tooltip("Канал спавнера диорам — нужен только для фильтра по текущей диораме")]
        [SerializeField] private DioramaSpawnerReference spawnerReference;

        [Header("Реакция на наведение")]
        [Tooltip("Канал этой панели для реакций объектов сцены (опционально)")]
        [SerializeField] private InventoryViewReference viewReference;

        private readonly Dictionary<ResourceValue, InventoryItemSlotView> slots = new();
        private readonly Dictionary<ResourceValue, Action<int>> handlers = new();
        private readonly HashSet<ResourceValue> dioramaItems = new();

        private DioramaSpawner spawner;

        private void OnEnable()
        {
            if (registry == null)
            {
                ServiceDebug.LogError($"{nameof(registry)} не назначен");
                return;
            }

            SubscribeItems();
            SubscribeSpawner();
            RebuildDioramaFilter();
            BuildSlots();

            if (viewReference != null) viewReference.Set(this);
        }

        private void OnDisable()
        {
            if (viewReference != null) viewReference.Clear();

            UnsubscribeItems();
            UnsubscribeSpawner();
            ClearSlots();
        }

        /// <inheritdoc/>
        public void SetItemNeeded(ResourceValue item, bool needed)
        {
            if (item != null && slots.TryGetValue(item, out var slot) && slot != null)
                slot.SetNeeded(needed);
        }

        private void SubscribeItems()
        {
            foreach (var item in registry.Entries)
            {
                if (item == null || handlers.ContainsKey(item)) continue;

                Action<int> handler = value => OnItemChanged(item, value);
                handlers[item] = handler;
                item.onValueChanged += handler;
            }
        }

        private void UnsubscribeItems()
        {
            foreach (var pair in handlers)
                if (pair.Key != null) pair.Key.onValueChanged -= pair.Value;

            handlers.Clear();
        }

        private void SubscribeSpawner()
        {
            if (spawnerReference == null) return;

            spawnerReference.onInitialized += OnSpawnerReady;
            spawnerReference.onReleased += OnSpawnerReleased;
            if (spawnerReference.HasValue) OnSpawnerReady(spawnerReference.Current);
        }

        private void UnsubscribeSpawner()
        {
            if (spawnerReference != null)
            {
                spawnerReference.onInitialized -= OnSpawnerReady;
                spawnerReference.onReleased -= OnSpawnerReleased;
            }

            OnSpawnerReleased();
        }

        private void OnSpawnerReady(DioramaSpawner value)
        {
            spawner = value;
            if (spawner != null) spawner.onFocusChanged += OnFocusChanged;

            if (!onlyCurrentDiorama) return;
            RebuildDioramaFilter();
            BuildSlots();
        }

        private void OnSpawnerReleased()
        {
            if (spawner != null) spawner.onFocusChanged -= OnFocusChanged;
            spawner = null;
        }

        private void OnFocusChanged(DioramaFocus _)
        {
            if (!onlyCurrentDiorama) return;
            RebuildDioramaFilter();
            BuildSlots();
        }

        private void RebuildDioramaFilter()
        {
            dioramaItems.Clear();
            if (!onlyCurrentDiorama) return;

            var sequence = spawner != null && spawner.Active != null ? spawner.Active.Sequence : null;
            if (sequence == null) return;

            foreach (var entry in sequence.Steps)
            {
                if (entry?.Gates == null) continue;

                foreach (var gate in entry.Gates)
                    if (gate is RequireResourceGate requirement && requirement.Resource != null)
                        dioramaItems.Add(requirement.Resource);
            }
        }

        private void BuildSlots()
        {
            ClearSlots();

            foreach (var item in registry.Entries)
            {
                if (item == null || !IsVisible(item)) continue;
                CreateSlot(item, animate: false);
            }
        }

        private void OnItemChanged(ResourceValue item, int newValue)
        {
            bool visible = IsVisible(item);
            bool hasSlot = slots.TryGetValue(item, out var slot);

            if (visible && !hasSlot)
            {
                CreateSlot(item, animate: true);
                return;
            }

            if (!visible && hasSlot)
            {
                RemoveSlot(item, slot);
                return;
            }

            if (!visible || !hasSlot) return;

            bool consumed = newValue < slot.Quantity;
            slot.SetQuantity(newValue);
            if (consumed) slot.PlayUse();
        }

        private bool IsVisible(ResourceValue item)
        {
            if (onlyCurrentDiorama && !dioramaItems.Contains(item)) return false;
            if (showOnlyOwned && item.Value <= 0) return false;
            return true;
        }

        private void CreateSlot(ResourceValue item, bool animate)
        {
            if (slotPrefab == null || container == null) return;

            var slot = Instantiate(slotPrefab, container);
            slot.Bind(item);
            slots[item] = slot;

            if (!animate) return;

            // Появление двигает RectTransform — без немедленного релейаута твин подхватит ещё не выставленную GridLayoutGroup позицию
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
            slot.PlayAppear();
        }

        private void RemoveSlot(ResourceValue item, InventoryItemSlotView slot)
        {
            slots.Remove(item);
            if (slot == null) return;

            slot.PlayDisappear(() => { if (slot != null) Destroy(slot.gameObject); });
        }

        private void ClearSlots()
        {
            foreach (var slot in slots.Values)
                if (slot != null) Destroy(slot.gameObject);

            slots.Clear();
        }
    }
}
