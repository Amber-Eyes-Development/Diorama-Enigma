using System;
using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Исполнитель последовательности (один на префаб)
    /// </summary>
    /// <remarks>
    /// Наблюдает завершённость шагов и управляет порядком групп и эффектами.=
    /// </remarks>
    public sealed class SequenceRunner : MonoBehaviour
    {
        /// <summary> Последовательность завершена </summary>
        public event Action onSequenceCompleted;

        [SerializeField] private Sequence sequence;
        [Tooltip("Запустить последовательность автоматически при включении объекта")]
        [SerializeField] private bool startOnEnable = true;

        private int currentGroupIndex;

        private readonly List<StepEntry> activeEntries = new();

        #region MonoBehaviour

        private void Awake()
        {
            if (sequence == null)
                ServiceDebug.LogError(this, "sequence не назначен");
        }

        private void OnEnable()
        {
            if (startOnEnable) StartSequence();
        }

        private void OnDisable() => StopSequence();

        #endregion

        /// <summary> Запустить последовательность (с возобновлением, если шаги сохраняемы) </summary>
        public void StartSequence()
        {
            if (sequence == null) return;

            StopSequence();

            bool resuming = AnyStepCompleted();

            if (!resuming)
                ResetAllSteps();

            ActivateAlwaysAvailableGroups();

            currentGroupIndex = -1;
            AdvanceToNextGroup();
        }

        /// <summary> Остановить выполнение и снять подписки </summary>
        public void StopSequence()
        {
            foreach (var entry in activeEntries)
                if (entry?.Step != null)
                    entry.Step.onCompletionChanged -= OnActiveStepCompletionChanged;

            activeEntries.Clear();

            if (sequence == null) return;
            foreach (var entry in sequence.Steps)
                entry?.Step?.SetActive(false);
        }

        /// <summary> Сбросить состояние всех шагов и перезапустить с начала </summary>
        public void RestartSequence()
        {
            if (sequence == null) return;

            StopSequence();
            ResetAllSteps();

            ActivateAlwaysAvailableGroups();

            currentGroupIndex = -1;
            AdvanceToNextGroup();
        }

        /// <summary> Перезапустить текущую группу шагов (со сбросом их значений) </summary>
        public void RestartStep()
        {
            if (sequence == null) return;

            StopSequence();
            ActivateAlwaysAvailableGroups();
            ResetGroupSteps(currentGroupIndex);
            ActivateGroup(currentGroupIndex);
        }

        #region Internal

        /// <summary> Активировать ввод для групп с доступностью Always </summary>
        private void ActivateAlwaysAvailableGroups()
        {
            foreach (var entry in sequence.Steps)
            {
                if (entry?.Step == null) continue;

                if (sequence.AvailabilityOf(entry.GroupIndex) == GroupAvailability.Always)
                    entry.Step.SetActive(true);
            }
        }

        private void AdvanceToNextGroup()
        {
            int maxGroup = GetMaxGroupIndex();

            do
            {
                currentGroupIndex++;
            }
            while (currentGroupIndex <= maxGroup && !GroupHasSteps(currentGroupIndex));

            if (currentGroupIndex > maxGroup)
            {
                onSequenceCompleted?.Invoke();
                return;
            }

            if (IsGroupAlreadyCompleted(currentGroupIndex))
            {
                RestoreCompletedGroup(currentGroupIndex);
                AdvanceToNextGroup();
                return;
            }

            ActivateGroup(currentGroupIndex);
        }

        private void ActivateGroup(int groupIndex)
        {
            foreach (var entry in sequence.Steps)
            {
                if (entry == null || entry.GroupIndex != groupIndex) continue;
                if (entry.Step == null)
                {
                    ServiceDebug.LogWarning(this, $"Шаг в группе {groupIndex} не назначен или не реализует {nameof(ISequenceStep)} — пропущен");
                    continue;
                }

                entry.Step.SetActive(true);
                activeEntries.Add(entry);
            }

            foreach (var entry in activeEntries)
                RunEffects(entry.ActivationEffects);

            foreach (var entry in activeEntries)
                entry.Step.onCompletionChanged += OnActiveStepCompletionChanged;

            CheckGroupCompletion();
        }

        private void OnActiveStepCompletionChanged(bool _) => CheckGroupCompletion();

        private void CheckGroupCompletion()
        {
            if (activeEntries.Count == 0) return;

            foreach (var entry in activeEntries)
                if (entry.Step == null || !entry.Step.IsCompleted) return;

            CompleteGroup();
        }

        private void CompleteGroup()
        {
            var completedEntries = new List<StepEntry>(activeEntries);
            activeEntries.Clear();

            foreach (var entry in completedEntries)
            {
                entry.Step.onCompletionChanged -= OnActiveStepCompletionChanged;
                entry.Step.SetActive(false);
            }

            foreach (var entry in completedEntries)
                RunEffects(entry.CompletionEffects);

            AdvanceToNextGroup();
        }

        private void RestoreCompletedGroup(int groupIndex)
        {
            foreach (var entry in sequence.Steps)
            {
                if (entry?.Step == null || entry.GroupIndex != groupIndex) continue;

                RunEffects(entry.ActivationEffects);
                RunEffects(entry.CompletionEffects);
            }
        }

        private void RunEffects(IReadOnlyList<SequenceStepEffect> effects)
        {
            if (effects == null) return;

            foreach (var effect in effects)
                effect?.Execute();
        }

        private void ResetAllSteps()
        {
            foreach (var entry in sequence.Steps)
                entry?.Step?.ResetState();
        }

        private void ResetGroupSteps(int groupIndex)
        {
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.GroupIndex == groupIndex)
                    entry.Step.ResetState();
        }

        private bool AnyStepCompleted()
        {
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.Step.IsCompleted) return true;

            return false;
        }

        private bool IsGroupAlreadyCompleted(int groupIndex)
        {
            bool any = false;

            foreach (var entry in sequence.Steps)
            {
                if (entry?.Step == null || entry.GroupIndex != groupIndex) continue;

                any = true;
                if (!entry.Step.IsCompleted) return false;
            }

            return any;
        }

        private int GetMaxGroupIndex()
        {
            int max = -1;

            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.GroupIndex > max)
                    max = entry.GroupIndex;

            return max;
        }

        private bool GroupHasSteps(int groupIndex)
        {
            foreach (var entry in sequence.Steps)
                if (entry?.Step != null && entry.GroupIndex == groupIndex)
                    return true;

            return false;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary> Текущий индекс активной группы </summary>
        public int Editor_CurrentGroupIndex => currentGroupIndex;

        /// <summary> Шаги, ожидающие завершения </summary>
        public IEnumerable<ISequenceStep> Editor_ActiveSteps
        {
            get
            {
                foreach (var entry in activeEntries)
                    if (entry?.Step != null) yield return entry.Step;
            }
        }

        /// <summary> Принудительно завершить активную группу </summary>
        public void Editor_ForceCompleteCurrentGroup()
        {
            if (activeEntries.Count > 0) CompleteGroup();
        }
#endif
    }
}
