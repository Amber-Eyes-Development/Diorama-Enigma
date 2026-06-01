using System;
using System.Collections;
using Extensions.Coroutines;
using Extensions.Events;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Обёртка над любым условием: добавляет задержку перед засчётом выполнения.
    /// Используется для синхронизации с анимациями — условие срабатывает,
    /// но шаг засчитывается только после <see cref="delaySeconds"/>.
    /// Провал передаётся немедленно без задержки.
    /// При отмене шага (Dispose) отложенный вызов отменяется.
    /// </summary>
    [CreateAssetMenu(menuName = "Riddles/Conditions/DelayedCondition", fileName = nameof(DelayedCondition))]
    public sealed class DelayedCondition : PuzzleCondition
    {
        /// <summary>Внутреннее условие</summary>
        public PuzzleCondition Inner => inner;

        /// <summary>Задержка в секундах между срабатыванием условия и засчётом шага</summary>
        public float DelaySeconds => delaySeconds;

        [SerializeField] private PuzzleCondition inner;
        [Tooltip("Задержка в секундах — обычно равна длине анимации объекта")]
        [SerializeField] private float delaySeconds = 1f;

        /// <inheritdoc/>
        public override IDisposable Activate(EventHub hub, Action onSatisfied, Action onFailed = null)
        {
            if (inner == null)
            {
                ServiceDebug.LogWarning<DelayedCondition>("inner условие не назначено");
                return new DelegateDisposable(null);
            }

            var delayTask = new CoroutineTask(RiddleContext.Instance);

            IDisposable innerDisposable = inner.Activate(
                hub,
                onSatisfied: () => delayTask.Start(DelayRoutine(onSatisfied)),
                onFailed: onFailed
            );

            return new DelegateDisposable(() =>
            {
                delayTask.Stop();
                innerDisposable.Dispose();
            });
        }

        /// <inheritdoc/>
        public override bool IsSatisfied(EventHub hub) => inner != null && inner.IsSatisfied(hub);

        private IEnumerator DelayRoutine(Action onSatisfied)
        {
            yield return new WaitForSeconds(delaySeconds);
            onSatisfied?.Invoke();
        }
    }
}
