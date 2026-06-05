using System;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Общая логика шага-значения: гейт, активность и отслеживание признака завершённости.
    /// Композируется в конкретные шаги (которые наследуют BoolValue / IntValue / StringValue),
    /// т.к. общий базовый класс невозможен из-за разных типов значения.
    /// </summary>
    [Serializable]
    public sealed class StepCompletionTracker
    {
        /// <summary> Изменение признака завершённости </summary>
        public event Action<bool> onCompletionChanged;

        /// <summary> Разблокировано ли изменение состояния (активен и гейт открыт) </summary>
        public bool IsUnlocked => isActive && (gate == null || gate.IsSatisfied());

        [Tooltip("Доп. условие, блокирующее изменение состояния (помимо порядка групп). Опционально")]
        [SerializeReference] private StepGate gate;

        [NonSerialized] private bool isActive;
        [NonSerialized] private bool lastCompleted;

        /// <summary> Инициализировать исходную завершённость (вызывать в OnEnable шага) </summary>
        /// <param name="completed">Завершённость при значении по-умолчанию</param>
        public void Initialize(bool completed)
        {
            isActive = false;
            lastCompleted = completed;
        }

        /// <summary> Активировать/деактивировать изменение состояния </summary>
        /// <param name="active">true — изменение разрешено</param>
        public void SetActive(bool active) => isActive = active;

        /// <summary> Уведомить подписчиков, если завершённость изменилась </summary>
        /// <param name="completed">Текущая завершённость</param>
        public void NotifyIfChanged(bool completed)
        {
            if (completed == lastCompleted) return;

            lastCompleted = completed;
            onCompletionChanged?.Invoke(completed);
        }
    }
}
