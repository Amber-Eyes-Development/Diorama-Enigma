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
        [Range(0, 99)]
        [SerializeField] private int normalCount = 3;
        [Range(0, 99)]
        [Tooltip("Приоритетных квестов")]
        [SerializeField] private int priorityCount = 1;

        [Header("Фильтр")]
        [Tooltip("Только квесты сфокусированной диорамы (иначе — весь блок)")]
        [SerializeField] private bool focusedOnly;
        [Tooltip("Показывать квесты линейных шагов")]
        [SerializeField] private bool showLinearQuests = true;
        [Tooltip("Показывать квесты Always-шагов (фоновых)")]
        [SerializeField] private bool showAlwaysQuests;

        [Header("Удержание")]
        [Tooltip("Держать выполненные квесты (метка «готово») до завершения приоритетного, стоящего за ними")]
        [SerializeField] private bool holdBeforePriority;

        private readonly Dictionary<string, QuestSlotView> slots = new();
        private readonly Dictionary<AbstractSequenceStep, Action<bool>> stepHandlers = new();
        private readonly List<StepGate> observedGates = new();

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
                    step.onUnlockChanged += handler;
                    ObserveGates(step);
                }
            }
        }

        private void UnsubscribeSteps()
        {
            foreach (var pair in stepHandlers)
            {
                if (pair.Key == null) continue;
                pair.Key.onCompletionChanged -= pair.Value;
                pair.Key.onUnlockChanged -= pair.Value;
            }

            stepHandlers.Clear();
            StopObservingGates();
        }

        private void ObserveGates(AbstractSequenceStep step)
        {
            var gates = step.QuestGates;
            if (gates == null) return;

            foreach (var gate in gates)
            {
                if (gate == null) continue;

                gate.StartObserving();
                gate.onSatisfactionChanged += OnGateChanged;
                observedGates.Add(gate);
            }
        }

        private void StopObservingGates()
        {
            foreach (var gate in observedGates)
            {
                if (gate == null) continue;

                gate.onSatisfactionChanged -= OnGateChanged;
                gate.StopObserving();
            }

            observedGates.Clear();
        }

        private void OnGateChanged() => Rebuild();

        private void Rebuild()
        {
            if (spawner == null || slotPrefab == null || container == null) return;

            var options = new QuestPanelOptions(
                normalCount, priorityCount, focusedOnly,
                holdBeforePriority, showLinearQuests, showAlwaysQuests);

            var desired = QuestSelector.Select(
                spawner.BlockDioramas, spawner.LiveInstances, spawner.Active, options);

            RemoveStaleSlots(desired);

            for (int i = 0; i < desired.Count; i++)
            {
                var quest = desired[i];
                bool focused = quest.Diorama == spawner.Active;

                if (!slots.TryGetValue(quest.StepId, out var slot) || slot == null)
                {
                    slot = CreateSlot(quest, focused);
                    if (slot == null) continue;
                }
                else
                {
                    slot.SetFocused(focused);
                }

                slot.transform.SetSiblingIndex(i);
                slot.SetDone(quest.Done);
            }
        }

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

        private QuestSlotView CreateSlot(ActiveQuest quest, bool focused)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Bind(quest);
            slots[quest.StepId] = slot;
            slot.PlayAppear(focused);
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
