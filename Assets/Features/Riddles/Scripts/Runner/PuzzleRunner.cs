using System;
using System.Collections.Generic;
using Extensions.Events;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Исполнитель последовательности загадки (один на префаб).
    /// Наблюдает завершённость шагов и управляет порядком групп и эффектами.
    /// </summary>
    public sealed class PuzzleRunner : MonoBehaviour
    {
        /// <summary> Последовательность завершена </summary>
        public event Action onSequenceCompleted;

        [SerializeField] private PuzzleSequence sequence;
        [Tooltip("Запустить последовательность автоматически при включении объекта")]
        [SerializeField] private bool startOnEnable = true;

        private int currentGroupIndex;
        private EventHub hub;

        private readonly List<PuzzleSequence.StepEntry> activeEntries = new();

        #region MonoBehaviour

        private void Awake()
        {
            if (sequence == null)
                ServiceDebug.LogWarning(this, "sequence не назначен");

            RiddleContext context = GetComponentInParent<RiddleContext>();
            if (context == null)
                ServiceDebug.LogWarning(this, "RiddleContext не найден в родителях — добавьте его на корень префаба загадки");
            else
                hub = context.Hub;
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
            if (hub == null)
            {
                ServiceDebug.LogError(this, "Хаб не инициализирован — нужен RiddleContext на префабе");
                return;
            }

            StopSequence();

            // Возобновление, если есть сохранённый прогресс шагов; иначе — чистый старт
            bool resuming = AnyStepCompleted();

            if (!resuming)
            {
                ResetAllSteps();
                hub.Publish(new PuzzleSequenceResetEvent(sequence.SequenceLabel));
            }

            currentGroupIndex = -1;
            AdvanceToNextGroup();
        }

        /// <summary> Остановить выполнение и снять подписки </summary>
        public void StopSequence()
        {
            foreach (var entry in activeEntries)
            {
                if (entry?.Step == null) continue;

                entry.Step.onCompletionChanged -= OnActiveStepCompletionChanged;
                entry.Step.SetActive(false);
            }

            activeEntries.Clear();
        }

        /// <summary> Сбросить состояние всех шагов и перезапустить с начала </summary>
        public void RestartSequence()
        {
            if (sequence == null || hub == null) return;

            StopSequence();
            ResetAllSteps();
            hub.Publish(new PuzzleSequenceResetEvent(sequence.SequenceLabel));

            currentGroupIndex = -1;
            AdvanceToNextGroup();
        }

        /// <summary> Перезапустить текущую группу шагов </summary>
        public void RestartStep()
        {
            if (sequence == null) return;

            StopSequence();
            ActivateGroup(currentGroupIndex);
        }

        #region Internal

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

            // Возобновление: уже завершённую группу не ждём, а восстанавливаем её побочные эффекты
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
                    ServiceDebug.LogWarning(this, $"Шаг в группе {groupIndex} не назначен или не реализует IPuzzleStep — пропущен");
                    continue;
                }

                entry.Step.SetActive(true);
                entry.Step.onCompletionChanged += OnActiveStepCompletionChanged;
                activeEntries.Add(entry);
            }

            foreach (var entry in activeEntries)
                RunEffects(entry.ActivationEffects);

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
            // Снимаем подписки и деактивируем до выполнения эффектов, чтобы избежать повторного входа
            var completedEntries = new List<PuzzleSequence.StepEntry>(activeEntries);
            activeEntries.Clear();

            foreach (var entry in completedEntries)
            {
                entry.Step.onCompletionChanged -= OnActiveStepCompletionChanged;
                entry.Step.SetActive(false);
            }

            foreach (var entry in completedEntries)
                RunEffects(entry.CompletionEffects);

            hub.Publish(new PuzzleStepCompletedEvent(sequence.SequenceLabel, currentGroupIndex));

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

        private void RunEffects(IReadOnlyList<PuzzleEffect> effects)
        {
            if (effects == null) return;

            foreach (var effect in effects)
                effect?.Execute(hub);
        }

        private void ResetAllSteps()
        {
            foreach (var entry in sequence.Steps)
                entry?.Step?.ResetState();
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
        public IEnumerable<IPuzzleStep> Editor_ActiveSteps
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
