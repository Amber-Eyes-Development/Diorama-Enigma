using System;
using System.Collections;
using Extensions.Coroutines;
using Extensions.Events;
using Extensions.Helpers;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Обёртка выполнения условия шага паззла с задержкой 
    /// </summary>
    [Serializable]
    public sealed class DelayedCondition : PuzzleCondition
    {
        /// <summary> Вложенное условие </summary>
        public PuzzleCondition Inner => inner;
        /// <summary> Задержка в секундах </summary>
        public float DelaySeconds => delaySeconds;

        [SerializeReference] private PuzzleCondition inner;
        [Tooltip("Задержка в секундах — обычно равна длине анимации объекта")]
        [Range(0f, 5f)]
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
