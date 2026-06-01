using System;
using System.Collections.Generic;
using Extensions.Data;
using Extensions.Events;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Исполнитель <see cref="PuzzleSequence"/>. Активирует группы шагов поочерёдно,
    /// дожидается выполнения всех условий каждого шага и переходит к следующей группе.
    /// При провале любого условия — приостанавливается и уведомляет внешние системы.
    /// Поддерживает сохранение и восстановление прогресса между сессиями.
    /// Полностью событийный — без корутин.
    /// </summary>
    public sealed class PuzzleRunner : MonoBehaviour
    {
        /// <summary>Вызывается при завершении всей последовательности</summary>
        public event Action onSequenceCompleted;

        /// <summary>
        /// Вызывается при провале любого шага текущей группы.
        /// Последовательность приостанавливается — вызвать <see cref="RestartStep"/>
        /// или <see cref="RestartSequence"/> для продолжения.
        /// </summary>
        public event Action<PuzzleStep> onStepFailed;

        [SerializeField] private PuzzleSequence sequence;
        [Tooltip("Запустить последовательность автоматически при включении объекта")]
        [SerializeField] private bool startOnEnable = true;

        [Header("Сохранение")]
        [Tooltip("Сохранять и восстанавливать прогресс между сессиями. " +
                 "Прогресс сохраняется по завершении каждой группы шагов.")]
        [SerializeField] private bool saveProgress = false;
        [Tooltip("Авто-генерируется при добавлении компонента. Не менять после выпуска.")]
        [SerializeField] private string saveKey;

        private int currentGroupIndex;
        private EventHub hub;

        private readonly Dictionary<PuzzleStep, List<IDisposable>> activeSubscriptions = new();
        private readonly Dictionary<PuzzleStep, int> pendingConditionCounts = new();

        #region Unity Lifecycle

        private void Awake()
        {
            if (sequence == null)
                ServiceDebug.LogWarning(this, "sequence не назначен");

            hub = RiddleContext.Instance.Hub;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(saveKey)) return;

            saveKey = IdGenerator.NewGuid();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private void OnEnable()
        {
            if (startOnEnable) StartSequence();
        }

        private void OnDisable() => StopSequence();

        #endregion

        /// <summary>
        /// Запустить последовательность. Если включено сохранение прогресса —
        /// возобновляет с последней завершённой группы без сброса объектов сцены.
        /// </summary>
        public void StartSequence()
        {
            StopSequence();

            int startGroup = LoadProgress();
            bool resumingFromSave = startGroup > 0;

            hub.ClearReplay<InteractableClickedEvent>();

            // Сброс объектов не нужен при возобновлении — они уже восстановлены из своих сохранений
            if (!resumingFromSave)
                hub.Publish(new PuzzleSequenceResetEvent(sequence.SequenceLabel));

            currentGroupIndex = startGroup - 1;
            AdvanceToNextGroup();
        }

        /// <summary>Остановить выполнение и снять все активные подписки</summary>
        public void StopSequence()
        {
            foreach (var subscriptions in activeSubscriptions.Values)
                foreach (var disposable in subscriptions)
                    disposable.Dispose();

            activeSubscriptions.Clear();
            pendingConditionCounts.Clear();
        }

        /// <summary>Полностью сбросить прогресс и перезапустить с первого шага</summary>
        public void RestartSequence()
        {
            EraseProgress();
            StopSequence();
            StartSequence();
        }

        /// <summary>
        /// Перезапустить текущую группу шагов после провала.
        /// Вызывать из обработчика <see cref="onStepFailed"/> или внешней системы.
        /// </summary>
        public void RestartStep()
        {
            StopSequence();
            ActivateGroup(currentGroupIndex);
        }

        #region Internal

        private void AdvanceToNextGroup()
        {
            int maxGroup = GetMaxGroupIndex();

            // Пропускаем пустые группы (дыры в нумерации GroupIndex), завершаемся только за последней группой
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

            ActivateGroup(currentGroupIndex);
        }

        private void ActivateGroup(int groupIndex)
        {
            foreach (var entry in sequence.Steps)
            {
                if (entry.GroupIndex != groupIndex) continue;
                if (entry.Step == null)
                {
                    ServiceDebug.LogWarning(this, $"Шаг с GroupIndex={groupIndex} имеет null Step — пропущен");
                    continue;
                }

                ActivateStep(entry.Step);
            }
        }

        private int GetMaxGroupIndex()
        {
            int max = -1;
            foreach (var entry in sequence.Steps)
                if (entry.Step != null && entry.GroupIndex > max)
                    max = entry.GroupIndex;

            return max;
        }

        private bool GroupHasSteps(int groupIndex)
        {
            foreach (var entry in sequence.Steps)
                if (entry.Step != null && entry.GroupIndex == groupIndex)
                    return true;

            return false;
        }

        private void ActivateStep(PuzzleStep step)
        {
            foreach (var effect in step.ActivationEffects)
                effect?.Execute(hub);

            int conditionCount = step.Conditions.Count;

            if (conditionCount == 0)
            {
                OnStepCompleted(step);
                return;
            }

            pendingConditionCounts[step] = conditionCount;
            activeSubscriptions[step] = new List<IDisposable>(conditionCount);

            foreach (var condition in step.Conditions)
            {
                if (condition == null)
                {
                    ServiceDebug.LogWarning(this, $"Шаг '{step.StepLabel}' содержит null условие — пропущено");
                    OnConditionSatisfied(step);
                    continue;
                }

                var capturedStep = step;
                bool conditionResolved = false;

                IDisposable subscription = condition.Activate(
                    hub,
                    onSatisfied: () =>
                    {
                        if (conditionResolved) return;
                        conditionResolved = true;
                        OnConditionSatisfied(capturedStep);
                    },
                    onFailed: () =>
                    {
                        if (conditionResolved) return;
                        conditionResolved = true;
                        OnConditionFailed(capturedStep);
                    });

                activeSubscriptions[step].Add(subscription);
            }
        }

        private void OnConditionSatisfied(PuzzleStep step)
        {
            if (!pendingConditionCounts.ContainsKey(step)) return;

            pendingConditionCounts[step]--;

            if (pendingConditionCounts[step] > 0) return;

            OnStepCompleted(step);
        }

        private void OnConditionFailed(PuzzleStep step)
        {
            StopSequence();

            foreach (var effect in step.FailureEffects)
                effect?.Execute(hub);

            hub.Publish(new PuzzleStepFailedEvent(sequence.SequenceLabel, currentGroupIndex));

            onStepFailed?.Invoke(step);
        }

        private void OnStepCompleted(PuzzleStep step)
        {
            if (activeSubscriptions.TryGetValue(step, out var subscriptions))
            {
                foreach (var d in subscriptions) d.Dispose();
                activeSubscriptions.Remove(step);
            }

            pendingConditionCounts.Remove(step);

            foreach (var effect in step.Effects)
                effect?.Execute(hub);

            hub.Publish(new PuzzleStepCompletedEvent(sequence.SequenceLabel, currentGroupIndex));

            if (activeSubscriptions.Count == 0 && pendingConditionCounts.Count == 0)
            {
                // Группа полностью завершена — сохранить прогресс перед переходом к следующей
                SaveProgress(currentGroupIndex);
                AdvanceToNextGroup();
            }
        }

#if UNITY_EDITOR

        /// <summary>Текущий индекс активной группы (только для редактора)</summary>
        public int Editor_CurrentGroupIndex => currentGroupIndex;

        /// <summary>Шаги, ожидающие выполнения в текущей группе (только для редактора)</summary>
        public IEnumerable<PuzzleStep> Editor_ActiveSteps => activeSubscriptions.Keys;

        /// <summary>
        /// Принудительно завершить все активные шаги текущей группы и перейти к следующей.
        /// Эффекты завершения выполняются, прогресс сохраняется. Только для редактора/тестирования.
        /// </summary>
        public void Editor_ForceCompleteCurrentGroup()
        {
            var steps = new List<PuzzleStep>(activeSubscriptions.Keys);
            foreach (var step in steps)
                OnStepCompleted(step);
        }

#endif

        private int LoadProgress()
        {
            if (!saveProgress || string.IsNullOrEmpty(saveKey)) return 0;

            return JsonSaveLoad.Load<int>(saveKey, defaultValue: 0);
        }

        private void SaveProgress(int completedGroupIndex)
        {
            if (!saveProgress || string.IsNullOrEmpty(saveKey)) return;

            // Сохраняем индекс следующей группы — с неё возобновимся при загрузке
            JsonSaveLoad.Save(completedGroupIndex + 1, saveKey);
        }

        private void EraseProgress()
        {
            if (!saveProgress || string.IsNullOrEmpty(saveKey)) return;

            JsonSaveLoad.Save(0, saveKey);
        }

        #endregion
    }
}
