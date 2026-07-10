using System;
using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using DioramaEnigma.Sequences;
using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Quests
{
    /// <summary>
    /// Панель заданий: показывает ближайшие по очереди квесты активного блока,
    /// динамически появляющиеся/исчезающие по мере прохождения шагов
    /// </summary>
    /// <remarks>
    /// Развязана от сцены: берёт спавнер из канала <see cref="DioramaSpawnerReference"/>.
    /// Квесты всех живых диорам блока (либо только сфокусированной), чужие — приглушены до возврата фокуса
    /// </remarks>
    public sealed class QuestPanelPresenter : RuntimeReferenceConsumer<DioramaSpawner>
    {
        [Header("UI")]
        [Tooltip("Контейнер слотов заданий")]
        [SerializeField] private Transform container;
        [Tooltip("Префаб слота задания")]
        [SerializeField] private QuestSlotView slotPrefab;

        [Header("Сколько показывать")]
        [Tooltip("Обычных квестов")]
        [SerializeField] private int normalCount = 3;
        [Tooltip("Приоритетных квестов (гарантированно попадают в список)")]
        [SerializeField] private int priorityCount = 1;

        [Header("Фильтр")]
        [Tooltip("Только квесты сфокусированной диорамы (иначе — весь блок)")]
        [SerializeField] private bool focusedOnly;

        private readonly Dictionary<string, QuestSlotView> slots = new();
        private readonly Dictionary<AbstractSequenceStep, Action<bool>> stepHandlers = new();

        private DioramaSpawner spawner;

        protected override void OnInitialized(DioramaSpawner value)
        {
            spawner = value;
            spawner.onFocusChanged += OnFocusChanged;
            spawner.onLiveInstancesChanged += OnLiveInstancesChanged;

            ResubscribeSteps();
            Rebuild();
        }

        protected override void OnReleased()
        {
            if (spawner != null)
            {
                spawner.onFocusChanged -= OnFocusChanged;
                spawner.onLiveInstancesChanged -= OnLiveInstancesChanged;
            }

            UnsubscribeSteps();
            ClearSlots();
            spawner = null;
        }

        private void OnFocusChanged(DioramaFocus _)
        {
            if (focusedOnly) Rebuild();
            else UpdateFocusStates();
        }

        private void OnLiveInstancesChanged()
        {
            ResubscribeSteps();
            Rebuild();
        }

        // Подписка на завершение квестовых шагов всех живых диорам: любое изменение пересобирает список
        private void ResubscribeSteps()
        {
            UnsubscribeSteps();
            if (spawner == null) return;

            foreach (var instance in spawner.LiveInstances)
            {
                var sequence = instance != null && instance.Definition != null ? instance.Definition.Sequence : null;
                if (sequence == null) continue;

                foreach (var entry in sequence.Steps)
                {
                    var step = entry?.Step;
                    if (step == null || !step.IsQuestStep || stepHandlers.ContainsKey(step)) continue;

                    Action<bool> handler = _ => Rebuild();
                    stepHandlers[step] = handler;
                    step.onCompletionChanged += handler;
                }
            }
        }

        private void UnsubscribeSteps()
        {
            foreach (var pair in stepHandlers)
                if (pair.Key != null) pair.Key.onCompletionChanged -= pair.Value;

            stepHandlers.Clear();
        }

        private void Rebuild()
        {
            if (spawner == null || slotPrefab == null || container == null) return;

            var desired = QuestSelector.Select(
                spawner.BlockDioramas, spawner.LiveInstances, spawner.Active,
                focusedOnly, normalCount, priorityCount);

            RemoveStaleSlots(desired);

            for (int i = 0; i < desired.Count; i++)
            {
                var quest = desired[i];

                if (!slots.TryGetValue(quest.StepId, out var slot) || slot == null)
                {
                    slot = CreateSlot(quest);
                    if (slot == null) continue;
                }

                slot.transform.SetSiblingIndex(i);
                slot.SetFocused(quest.Diorama == spawner.Active);
            }
        }

        // Убрать слоты, которых больше нет в выборке
        private void RemoveStaleSlots(List<ActiveQuest> desired)
        {
            var stale = new List<string>();

            foreach (var stepId in slots.Keys)
            {
                bool present = false;
                foreach (var quest in desired)
                    if (quest.StepId == stepId) { present = true; break; }

                if (!present) stale.Add(stepId);
            }

            foreach (var stepId in stale)
            {
                var slot = slots[stepId];
                slots.Remove(stepId);
                if (slot != null) slot.PlayDisappear(() => { if (slot != null) Destroy(slot.gameObject); });
            }
        }

        private QuestSlotView CreateSlot(ActiveQuest quest)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Bind(quest);
            slots[quest.StepId] = slot;
            slot.PlayAppear();
            return slot;
        }

        private void UpdateFocusStates()
        {
            if (spawner == null) return;

            foreach (var slot in slots.Values)
                if (slot != null) slot.SetFocused(slot.Diorama == spawner.Active);
        }

        private void ClearSlots()
        {
            foreach (var slot in slots.Values)
                if (slot != null) Destroy(slot.gameObject);

            slots.Clear();
        }
    }
}
