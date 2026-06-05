using System;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Общая логика шага-значения: гейт, активность и отслеживание признака завершённости
    /// </summary>
    /// <remarks>
    /// Композируется в конкретные шаги (наследники BoolValue / IntValue / StringValue),
    /// т.к. общий базовый класс невозможен из-за разных типов значения.
    /// </remarks>
    [Serializable]
    public sealed class StepCompletionTracker
    {
        /// <summary> Изменение признака завершённости </summary>
        public event Action<bool> onCompletionChanged;

        /// <summary> Разблокировано ли изменение состояния (активен и гейт открыт) </summary>
        public bool IsUnlocked => isActive && (gate == null || gate.IsSatisfied());

        [Tooltip("Доп. условие, блокирующее изменение состояния (помимо порядка групп). Опционально")]
        [SerializeReference] private StepGate gate;

        private bool isActive;
        private bool lastCompleted;

        /// <summary> Инициализировать исходную завершённость (вызывать в OnEnable шага) </summary>
        public void Initialize(bool completed)
        {
            isActive = false;
            lastCompleted = completed;
        }

        /// <summary> Активировать/деактивировать изменение состояния </summary>
        public void SetActive(bool active) => isActive = active;

        /// <summary> Уведомить подписчиков, если завершённость изменилась </summary>
        public void NotifyIfChanged(bool completed)
        {
            if (completed == lastCompleted) return;

            lastCompleted = completed;
            onCompletionChanged?.Invoke(completed);
        }

#if UNITY_EDITOR
        public void EditorValidate(UnityEngine.Object context)
        {
            if (gate is RequireStepsCompletedGate reqGate)
                reqGate.EditorValidate(context);
        }
#endif
    }
}
