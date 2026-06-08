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

        [Header("Условие старта (опционально)")]
        [Tooltip("Если задан — старт по состоянию этого шага (вместо startOnEnable)")]
        [SerializeField] private SequenceStep startStep;
        [Tooltip("Состояние/событие шага старта, по которому запускается последовательность")]
        [SerializeField] private TriggerKind startTrigger = TriggerKind.Completed;

        private int currentGroupIndex;
        private bool isCompleted;

        /// <summary> Активный шаг группы с подписками на события (для эффектов и завершения группы) </summary>
        private sealed class ActiveStep
        {
            public StepEntry Entry;
            public Action<bool> CompletionHandler;
            public Action<bool> UnlockHandler;
        }

        private readonly List<ActiveStep> activeSteps = new();

        #region MonoBehaviour

        private void Awake()
        {
            if (sequence == null)
                ServiceDebug.LogError(this, "sequence не назначен");
        }

        private void OnEnable()
        {
            if (startStep != null) BeginStartWatch();
            else if (startOnEnable) StartSequence();
        }

        private void OnDisable()
        {
            StopStartWatch();
            StopSequence();
        }

        #endregion

        #region Start condition

        private void BeginStartWatch()
        {
            if (startTrigger.IsSatisfiedBy(startStep.IsCompleted, startStep.IsUnlocked))
            {
                StartSequence();
                return;
            }

            startStep.onCompletionChanged += OnStartCompletionChanged;
            startStep.onUnlockChanged += OnStartUnlockChanged;
        }

        private void StopStartWatch()
        {
            if (startStep == null) return;

            startStep.onCompletionChanged -= OnStartCompletionChanged;
            startStep.onUnlockChanged -= OnStartUnlockChanged;
        }

        private void OnStartCompletionChanged(bool completed) =>
            TryStartByTrigger(completed ? TriggerKind.Completed : TriggerKind.NotCompleted);

        private void OnStartUnlockChanged(bool unlocked) =>
            TryStartByTrigger(unlocked ? TriggerKind.Unlocked : TriggerKind.Locked);

        private void TryStartByTrigger(TriggerKind fired)
        {
            if (!startTrigger.Responds(fired)) return;

            StopStartWatch();
            StartSequence();
        }

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
            isCompleted = false;
            AdvanceToNextGroup();
        }

        /// <summary> Остановить выполнение и снять подписки </summary>
        public void StopSequence()
        {
            DeactivateActiveSteps();

            if (sequence == null) return;
            foreach (var entry in sequence.Steps)
                entry?.Step?.SetActive(false);
        }

        /// <summary> Откатить (сбросить к исходному значению) один шаг </summary>
        public void ResetStep(AbstractSequenceStep step) => step?.ResetState();

        /// <summary> Откатить все шаги группы к исходным значениям </summary>
        public void ResetGroup(int groupIndex)
        {
            if (sequence == null) return;
            ResetGroupSteps(groupIndex);
        }

        /// <summary> Откатить все шаги последовательности к исходным значениям </summary>
        public void ResetSequence()
        {
            if (sequence == null) return;
            ResetAllSteps();
        }

        /// <summary> Сбросить состояние всех шагов и перезапустить с начала </summary>
        public void RestartSequence()
        {
            if (sequence == null) return;

            StopSequence();
            ResetAllSteps();

            ActivateAlwaysAvailableGroups();

            currentGroupIndex = -1;
            isCompleted = false;
            AdvanceToNextGroup();
        }

        /// <summary> Перезапустить текущую группу шагов (со сбросом их значений) </summary>
        public void RestartStep()
        {
            if (sequence == null) return;

            StopSequence();
            isCompleted = false;
            ActivateAlwaysAvailableGroups();
            ResetGroupSteps(currentGroupIndex);
            ActivateGroup(currentGroupIndex);
        }

        #region Internal

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
                isCompleted = true;
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
                    ServiceDebug.LogWarning(this, $"Шаг в группе {groupIndex} не назначен — пропущен");
                    continue;
                }

                var active = new ActiveStep { Entry = entry };
                active.CompletionHandler = completed => OnStepCompletionChanged(active, completed);
                active.UnlockHandler = unlocked => OnStepUnlockChanged(active, unlocked);
                activeSteps.Add(active);

                entry.Step.onCompletionChanged += active.CompletionHandler;
                entry.Step.onUnlockChanged += active.UnlockHandler;
                entry.Step.SetActive(true);
            }

            CheckGroupCompletion();
        }

        private void OnStepCompletionChanged(ActiveStep active, bool completed)
        {
            RunMatchingEffects(active.Entry, completed ? TriggerKind.Completed : TriggerKind.NotCompleted);
            CheckGroupCompletion();
        }

        private void OnStepUnlockChanged(ActiveStep active, bool unlocked)
        {
            RunMatchingEffects(active.Entry, unlocked ? TriggerKind.Unlocked : TriggerKind.Locked);
        }

        private void CheckGroupCompletion()
        {
            if (activeSteps.Count == 0) return;

            foreach (var active in activeSteps)
                if (active.Entry.Step == null || !active.Entry.Step.IsCompleted) return;

            CompleteGroup();
        }

        private void CompleteGroup()
        {
            var completed = new List<ActiveStep>(activeSteps);
            activeSteps.Clear();

            foreach (var active in completed)
            {
                Unsubscribe(active);

                if (!sequence.InteractableAfterCompletionOf(active.Entry.GroupIndex))
                    active.Entry.Step.SetActive(false);
            }

            AdvanceToNextGroup();
        }

        private void RestoreCompletedGroup(int groupIndex)
        {
            bool interactable = sequence.InteractableAfterCompletionOf(groupIndex);

            foreach (var entry in sequence.Steps)
            {
                if (entry?.Step == null || entry.GroupIndex != groupIndex) continue;

                RunMatchingEffects(entry, TriggerKind.Completed);

                if (interactable)
                    entry.Step.SetActive(true);
            }
        }

        private static void RunMatchingEffects(StepEntry entry, TriggerKind trigger)
        {
            foreach (var effectEntry in entry.Effects)
                if (effectEntry != null && effectEntry.Trigger.Responds(trigger))
                    effectEntry.Effect?.Execute();
        }

        /// <summary> Снять подписки и деактивировать текущие активные шаги </summary>
        private void DeactivateActiveSteps()
        {
            foreach (var active in activeSteps)
            {
                Unsubscribe(active);
                active.Entry?.Step?.SetActive(false);
            }

            activeSteps.Clear();
        }

        private static void Unsubscribe(ActiveStep active)
        {
            if (active.Entry?.Step == null) return;

            active.Entry.Step.onCompletionChanged -= active.CompletionHandler;
            active.Entry.Step.onUnlockChanged -= active.UnlockHandler;
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
        /// <summary> Назначенная последовательность (для редакторских окон) </summary>
        public Sequence Editor_Sequence => sequence;

        /// <summary> Текущий индекс активной группы </summary>
        public int Editor_CurrentGroupIndex => currentGroupIndex;

        /// <summary> Последовательность полностью пройдена (все группы завершены) </summary>
        public bool Editor_IsCompleted => isCompleted;

        /// <summary> Шаги, ожидающие завершения </summary>
        public IEnumerable<AbstractSequenceStep> Editor_ActiveSteps
        {
            get
            {
                foreach (var active in activeSteps)
                    if (active.Entry?.Step != null) yield return active.Entry.Step;
            }
        }

        /// <summary> Принудительно завершить активную группу </summary>
        public void Editor_ForceCompleteCurrentGroup()
        {
            if (activeSteps.Count > 0) CompleteGroup();
        }

        /// <summary> Есть ли предыдущая группа с шагами (можно ли шагнуть назад) </summary>
        public bool Editor_HasPreviousGroup => PreviousGroupWithSteps() >= 0;

        /// <summary> Перейти к следующей группе (принудительно завершив текущую) </summary>
        public void Editor_GoToNextGroup() => Editor_ForceCompleteCurrentGroup();

        /// <summary> Откатить текущую группу и вернуться к предыдущей (с её повторной активацией) </summary>
        public void Editor_GoToPreviousGroup()
        {
            if (sequence == null) return;

            int prev = PreviousGroupWithSteps();
            if (prev < 0) return;

            int current = currentGroupIndex;
            DeactivateActiveSteps();
            ResetGroupSteps(current); 

            isCompleted = false;
            currentGroupIndex = prev;
            ResetGroupSteps(prev);  
            ActivateGroup(prev);
        }

        /// <summary> Ближайшая предыдущая группа, содержащая шаги (или -1) </summary>
        private int PreviousGroupWithSteps()
        {
            if (sequence == null) return -1;

            for (int g = currentGroupIndex - 1; g >= 0; g--)
                if (GroupHasSteps(g)) return g;

            return -1;
        }
#endif
    }
}
